using MediatR;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Categories;
using VISSTA.Application.Features.Products;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers;

[Route("collection")]
public sealed class CollectionController(IMediator mediator) : Controller
{
    [HttpGet("")]
    [HttpGet("{slug}")]
    public async Task<IActionResult> Index(string? slug, CancellationToken cancellationToken)
    {
        var allCategories = await mediator.Send(new GetCategoryListQuery(), cancellationToken);

        // Find the requested category by slug (or show all if no slug)
        var activeCategory = slug is not null
            ? allCategories.FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
            : null;

        // Gather category IDs to filter products: if a parent cat is selected, include children too
        int? filterCategoryId = null;
        if (activeCategory is not null)
        {
            filterCategoryId = activeCategory.Id;
        }

        // Fetch products for the active category (or all if none selected)
        var products = await mediator.Send(
            new GetProductListQuery(filterCategoryId, null, null, null, null, false),
            cancellationToken);

        var viewModel = new CollectionViewModel(allCategories, activeCategory, products);
        return View(viewModel);
    }
}
