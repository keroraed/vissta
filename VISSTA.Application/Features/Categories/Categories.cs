using FluentValidation;
using MediatR;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;

namespace VISSTA.Application.Features.Categories;

public sealed record GetCategoryListQuery() : IRequest<IReadOnlyCollection<CategoryDto>>;
public sealed record GetCategoryByIdQuery(int Id) : IRequest<CategoryDto?>;
public sealed record CreateCategoryCommand(string Name, string Slug, int? ParentCategoryId, string? ImageUrl, bool ShowOnHomePage, string? SizeChartImageUrl, string? WashingInstructionsImageUrl) : IRequest<int>;
public sealed record UpdateCategoryCommand(int Id, string Name, string Slug, int? ParentCategoryId, string? ImageUrl, bool ShowOnHomePage, string? SizeChartImageUrl, string? WashingInstructionsImageUrl) : IRequest<bool>;
public sealed record DeleteCategoryCommand(int Id) : IRequest<bool>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(140);
    }
}

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(140);
    }
}

public sealed class CategoryHandlers(IRepository<Category> categories, IProductRepository products, IUnitOfWork unitOfWork) :
    IRequestHandler<GetCategoryListQuery, IReadOnlyCollection<CategoryDto>>,
    IRequestHandler<GetCategoryByIdQuery, CategoryDto?>,
    IRequestHandler<CreateCategoryCommand, int>,
    IRequestHandler<UpdateCategoryCommand, bool>,
    IRequestHandler<DeleteCategoryCommand, bool>
{
    public Task<IReadOnlyCollection<CategoryDto>> Handle(GetCategoryListQuery request, CancellationToken cancellationToken)
    {
        var items = categories.QueryReadOnly()
            .OrderBy(x => x.Name)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Slug, x.ParentCategoryId, x.ImageUrl, x.ShowOnHomePage, x.SizeChartImageUrl, x.WashingInstructionsImageUrl))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CategoryDto>>(items);
    }

    public async Task<CategoryDto?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.Id, cancellationToken);
        return category is null ? null : new CategoryDto(category.Id, category.Name, category.Slug, category.ParentCategoryId, category.ImageUrl, category.ShowOnHomePage, category.SizeChartImageUrl, category.WashingInstructionsImageUrl);
    }

    public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category(request.Name, request.Slug, request.ParentCategoryId, request.ImageUrl, request.ShowOnHomePage, request.SizeChartImageUrl, request.WashingInstructionsImageUrl);
        await categories.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return category.Id;
    }

    public async Task<bool> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return false;
        }

        category.Update(request.Name, request.Slug, request.ParentCategoryId, request.ImageUrl, request.ShowOnHomePage, request.SizeChartImageUrl, request.WashingInstructionsImageUrl);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var hasProducts = products.QueryReadOnly().Any(x => x.CategoryId == request.Id);
        if (hasProducts)
        {
            return false;
        }

        var category = await categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return false;
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
