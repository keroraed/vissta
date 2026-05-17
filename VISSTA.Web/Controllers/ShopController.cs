using MediatR;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Products;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers;

public sealed class ShopController(IMediator mediator) : Controller
{
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "categoryId", "minPrice", "maxPrice", "sort", "search" })]
    public async Task<IActionResult> Index(int? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, string? search, CancellationToken cancellationToken)
    {
        var products = await mediator.Send(new GetProductListQuery(categoryId, minPrice, maxPrice, sort, search), cancellationToken);

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return PartialView("_ProductGrid", products);
        }

        return View(new ShopViewModel(products, categoryId, minPrice, maxPrice, sort, search));
    }

    [HttpGet("/shop/{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        var product = await mediator.Send(new GetProductBySlugQuery(slug), cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var related = await mediator.Send(new GetProductListQuery(null, null, null, null, null), cancellationToken);
        return View(new ProductDetailViewModel(product, related.Where(x => x.Id != product.Id).Take(4).ToList()));
    }
}
