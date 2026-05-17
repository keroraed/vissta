using FluentValidation;
using MediatR;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Domain.ValueObjects;

namespace VISSTA.Application.Features.Products;

public sealed record GetProductListQuery(int? CategoryId, decimal? MinPrice, decimal? MaxPrice, string? Sort, string? Search) : IRequest<IReadOnlyCollection<ProductListDto>>;
public sealed record GetFeaturedProductsQuery(int Count = 3) : IRequest<IReadOnlyCollection<ProductListDto>>;
public sealed record GetProductByIdQuery(int Id) : IRequest<ProductDetailDto?>;
public sealed record GetProductBySlugQuery(string Slug) : IRequest<ProductDetailDto?>;
public sealed record SearchProductsQuery(string Term) : IRequest<IReadOnlyCollection<SearchSuggestionDto>>;

public sealed record CreateProductCommand(string Name, string Slug, string Description, decimal Price, int Stock, string Sku, int CategoryId, bool IsFeatured) : IRequest<int>;
public sealed record UpdateProductCommand(int Id, string Name, string Slug, string Description, decimal Price, int CategoryId, bool IsActive, bool IsFeatured) : IRequest<bool>;
public sealed record DeleteProductCommand(int Id) : IRequest<bool>;
public sealed record UpdateStockCommand(int ProductId, int Stock) : IRequest<bool>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
    }
}

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

public sealed class ProductHandlers(IProductRepository products, IUnitOfWork unitOfWork) :
    IRequestHandler<GetProductListQuery, IReadOnlyCollection<ProductListDto>>,
    IRequestHandler<GetFeaturedProductsQuery, IReadOnlyCollection<ProductListDto>>,
    IRequestHandler<GetProductByIdQuery, ProductDetailDto?>,
    IRequestHandler<GetProductBySlugQuery, ProductDetailDto?>,
    IRequestHandler<SearchProductsQuery, IReadOnlyCollection<SearchSuggestionDto>>,
    IRequestHandler<CreateProductCommand, int>,
    IRequestHandler<UpdateProductCommand, bool>,
    IRequestHandler<DeleteProductCommand, bool>,
    IRequestHandler<UpdateStockCommand, bool>
{
    public Task<IReadOnlyCollection<ProductListDto>> Handle(GetProductListQuery request, CancellationToken cancellationToken)
    {
        var query = products.QueryReadOnly().Where(x => x.IsActive);

        if (request.CategoryId is not null)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId);
        }

        if (request.MinPrice is not null)
        {
            query = query.Where(x => x.Price.Amount >= request.MinPrice);
        }

        if (request.MaxPrice is not null)
        {
            query = query.Where(x => x.Price.Amount <= request.MaxPrice);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Name.Contains(request.Search) || x.Description.Contains(request.Search));
        }

        query = request.Sort switch
        {
            "price-asc" => query.OrderBy(x => x.Price.Amount),
            "price-desc" => query.OrderByDescending(x => x.Price.Amount),
            "newest" => query.OrderByDescending(x => x.Id),
            _ => query.OrderByDescending(x => x.IsFeatured).ThenByDescending(x => x.UnitsSold)
        };

        return Task.FromResult<IReadOnlyCollection<ProductListDto>>(query.Select(ToListDto).ToList());
    }

    public Task<IReadOnlyCollection<ProductListDto>> Handle(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
    {
        var items = products.QueryReadOnly()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.UnitsSold)
            .Take(request.Count)
            .Select(ToListDto)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<ProductListDto>>(items);
    }

    public async Task<ProductDetailDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken);
        return product is null ? null : ToDetailDto(product);
    }

    public async Task<ProductDetailDto?> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var product = await products.GetBySlugAsync(request.Slug, cancellationToken);
        return product is null ? null : ToDetailDto(product);
    }

    public Task<IReadOnlyCollection<SearchSuggestionDto>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var term = request.Term.Trim();
        if (term.Length < 2)
        {
            return Task.FromResult<IReadOnlyCollection<SearchSuggestionDto>>([]);
        }

        var results = products.QueryReadOnly()
            .Where(x => x.IsActive && x.Name.Contains(term))
            .OrderByDescending(x => x.UnitsSold)
            .Take(8)
            .Select(x => new SearchSuggestionDto(
                x.Id,
                x.Name,
                x.Slug,
                x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? "/assets/product-white-polo.webp",
                x.Price.Amount))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<SearchSuggestionDto>>(results);
    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(request.Name, request.Slug, request.Description, new Money(request.Price), request.Stock, request.Sku, request.CategoryId, request.IsFeatured);
        await products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return false;
        }

        product.Update(request.Name, request.Slug, request.Description, new Money(request.Price), request.CategoryId, request.IsActive, request.IsFeatured);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return false;
        }

        products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(UpdateStockCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return false;
        }

        product.UpdateStock(request.Stock);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProductListDto ToListDto(Product x) => new(
        x.Id,
        x.Name,
        x.Slug,
        x.Price.Amount,
        x.Price.Currency,
        x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? "/assets/product-white-polo.webp",
        x.Category == null ? "VISSTA" : x.Category.Name,
        x.IsFeatured,
        x.Stock);

    private static ProductDetailDto ToDetailDto(Product x) => new(
        x.Id,
        x.Name,
        x.Slug,
        x.Description,
        x.Price.Amount,
        x.Price.Currency,
        x.Stock,
        x.SKU,
        x.Category == null ? "VISSTA" : x.Category.Name,
        x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => new ProductImageDto(i.Id, i.Url, i.IsPrimary, i.DisplayOrder)).ToList(),
        x.Reviews.Where(r => r.IsApproved).Select(r => new ReviewDto(r.Id, r.Customer == null ? "VISSTA Customer" : r.Customer.FullName, r.Rating, r.Body, r.CreatedAt)).ToList());
}
