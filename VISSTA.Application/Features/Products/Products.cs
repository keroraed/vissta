using FluentValidation;
using MediatR;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Domain.ValueObjects;

namespace VISSTA.Application.Features.Products;

public sealed record GetProductListQuery(int? CategoryId, decimal? MinPrice, decimal? MaxPrice, string? Sort, string? Search, bool IncludeInactive = false) : IRequest<IReadOnlyCollection<ProductListDto>>;
public sealed record GetFeaturedProductsQuery(int Count = 3) : IRequest<IReadOnlyCollection<ProductListDto>>;
public sealed record GetHomePageProductsQuery(int Count = 4) : IRequest<IReadOnlyCollection<ProductListDto>>;
public sealed record GetProductByIdQuery(int Id) : IRequest<ProductDetailDto?>;
public sealed record GetProductBySlugQuery(string Slug) : IRequest<ProductDetailDto?>;
public sealed record SearchProductsQuery(string Term) : IRequest<IReadOnlyCollection<SearchSuggestionDto>>;

public sealed record CreateProductCommand(string Name, string Slug, string Description, decimal Price, IReadOnlyCollection<ProductSizeStockInputDto> SizeStocks, string Sku, int CategoryId, bool IsFeatured, bool ShowOnHomePage, string? DiscountType, decimal? DiscountValue, IReadOnlyCollection<string> ImageUrls) : IRequest<int>;
public sealed record UpdateProductCommand(int Id, string Name, string Slug, string Description, decimal Price, int CategoryId, bool IsActive, bool IsFeatured, bool ShowOnHomePage, string? DiscountType, decimal? DiscountValue, IReadOnlyCollection<string> ImageUrls, IReadOnlyCollection<int> RemoveImageIds, IReadOnlyCollection<ProductSizeStockInputDto> SizeStocks) : IRequest<bool>;
public sealed record DeleteProductCommand(int Id) : IRequest<bool>;
public sealed record SetDiscountCommand(int ProductId, string? DiscountType, decimal? DiscountValue) : IRequest<bool>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.SizeStocks).NotEmpty();
        RuleForEach(x => x.SizeStocks).ChildRules(stock =>
        {
            stock.RuleFor(s => s.Stock).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.SizeStocks).NotEmpty();
        RuleForEach(x => x.SizeStocks).ChildRules(stock =>
        {
            stock.RuleFor(s => s.Stock).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class ProductHandlers(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IRepository<CartItem> cartItems,
    IRepository<NewsletterCampaignProduct> campaignProducts) :
    IRequestHandler<GetProductListQuery, IReadOnlyCollection<ProductListDto>>,
    IRequestHandler<GetFeaturedProductsQuery, IReadOnlyCollection<ProductListDto>>,
    IRequestHandler<GetHomePageProductsQuery, IReadOnlyCollection<ProductListDto>>,
    IRequestHandler<GetProductByIdQuery, ProductDetailDto?>,
    IRequestHandler<GetProductBySlugQuery, ProductDetailDto?>,
    IRequestHandler<SearchProductsQuery, IReadOnlyCollection<SearchSuggestionDto>>,
    IRequestHandler<CreateProductCommand, int>,
    IRequestHandler<UpdateProductCommand, bool>,
    IRequestHandler<DeleteProductCommand, bool>,
    IRequestHandler<SetDiscountCommand, bool>
{
    public Task<IReadOnlyCollection<ProductListDto>> Handle(GetProductListQuery request, CancellationToken cancellationToken)
    {
        var query = products.QueryReadOnly();
        if (!request.IncludeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (request.CategoryId is not null)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId || (x.Category != null && x.Category.ParentCategoryId == request.CategoryId));
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

    public Task<IReadOnlyCollection<ProductListDto>> Handle(GetHomePageProductsQuery request, CancellationToken cancellationToken)
    {
        var query = products.QueryReadOnly()
            .Where(x => x.IsActive && x.ShowOnHomePage)
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.UnitsSold);

        var items = query.Take(request.Count).Select(ToListDto).ToList();

        // Fallback to newest products if no home-page products are flagged
        if (items.Count == 0)
        {
            items = products.QueryReadOnly()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Id)
                .Take(request.Count)
                .Select(ToListDto)
                .ToList();
        }

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
        var product = new Product(request.Name, request.Slug, request.Description, new Money(request.Price), 0, request.Sku, request.CategoryId, request.IsFeatured);
        product.SetShowOnHomePage(request.ShowOnHomePage);
        product.SetDiscount(request.DiscountType, request.DiscountValue);
        foreach (var sizeStock in request.SizeStocks)
        {
            product.AddSizeStock(sizeStock.SizeId, sizeStock.Stock, sizeStock.IsAvailable);
        }
        await products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (request.ImageUrls.Count > 0)
        {
            product.ReplaceImages(request.ImageUrls);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return product.Id;
    }

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return false;
        }

        product.Update(request.Name, request.Slug, request.Description, new Money(request.Price), request.CategoryId, request.IsActive, request.IsFeatured, request.ShowOnHomePage, request.DiscountType, request.DiscountValue);
        product.UpdateSizeStocks(request.SizeStocks.Select(x => (x.SizeId, x.Stock, x.IsAvailable)));
        if (request.ImageUrls.Count > 0 || request.RemoveImageIds.Count > 0)
        {
            var removeSet = request.RemoveImageIds.Count > 0 ? request.RemoveImageIds.ToHashSet() : null;
            var remainingUrls = product.Images
                .Where(image => removeSet is null || !removeSet.Contains(image.Id))
                .OrderBy(image => image.DisplayOrder)
                .Select(image => image.Url);
            var updatedUrls = remainingUrls.Concat(request.ImageUrls).ToList();
            product.ReplaceImages(updatedUrls);
        }
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

        if (!product.IsActive)
        {
            var cartItemsList = cartItems.Query().Where(x => x.ProductId == request.Id).ToList();
            foreach (var item in cartItemsList)
            {
                cartItems.Remove(item);
            }

            var campaignProductsList = campaignProducts.Query().Where(x => x.ProductId == request.Id).ToList();
            foreach (var cp in campaignProductsList)
            {
                campaignProducts.Remove(cp);
            }

            products.Remove(product);
        }
        else
        {
            product.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(SetDiscountCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return false;
        }

        product.SetDiscount(request.DiscountType, request.DiscountValue);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProductListDto ToListDto(Product x) => new(
        x.Id,
        x.Name,
        x.Slug,
        x.Price.Amount,
        x.EffectivePrice,
        x.Price.Currency,
        x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? "/assets/product-white-polo.webp",
        x.Category == null ? "VISSTA" : x.Category.Name,
        x.IsFeatured,
        x.ShowOnHomePage,
        x.DiscountType,
        x.DiscountValue,
        x.Stock,
        x.IsActive);

    private static ProductDetailDto ToDetailDto(Product x) => new(
        x.Id,
        x.Name,
        x.Slug,
        x.Description,
        x.Price.Amount,
        x.EffectivePrice,
        x.SavedAmount,
        x.DiscountType,
        x.DiscountValue,
        x.Price.Currency,
        x.Stock,
        x.SKU,
        x.CategoryId,
        x.Category == null ? "VISSTA" : x.Category.Name,
        x.IsActive,
        x.IsFeatured,
        x.ShowOnHomePage,
        x.SizeStocks
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.Size != null ? s.Size.DisplayOrder : 0)
            .Select(s => new ProductSizeStockDto(s.Size?.Name ?? string.Empty, s.Stock))
            .ToList(),
        x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => new ProductImageDto(i.Id, i.Url, i.IsPrimary, i.DisplayOrder)).ToList(),
        x.Reviews.Where(r => r.IsApproved).Select(r => new ReviewDto(
            r.Id,
            r.Customer == null ? "VISSTA Customer" : r.Customer.FullName,
            r.Rating,
            r.Body,
            r.CreatedAt,
            r.ProductId,
            x.Name,
            x.Slug,
            r.IsApproved)).ToList(),
        x.Category?.SizeChartImageUrl,
        x.Category?.WashingInstructionsImageUrl);
}
