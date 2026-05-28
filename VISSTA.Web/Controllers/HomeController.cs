using MediatR;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Categories;
using VISSTA.Application.Features.Products;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers;

public sealed class HomeController(IMediator mediator) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var featured = await mediator.Send(new GetFeaturedProductsQuery(3), cancellationToken);
        var collection = await mediator.Send(new GetHomePageProductsQuery(4), cancellationToken);
        var categories = await mediator.Send(new GetCategoryListQuery(), cancellationToken);
        return View(new HomeViewModel(featured, collection.ToList(), categories));
    }

    [HttpGet("/About")]
    public IActionResult About() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
