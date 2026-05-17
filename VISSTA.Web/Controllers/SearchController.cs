using MediatR;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Products;

namespace VISSTA.Web.Controllers;

public sealed class SearchController(IMediator mediator) : Controller
{
    [HttpGet("/api/search")]
    public async Task<IActionResult> Suggestions(string q, CancellationToken cancellationToken) =>
        Json(await mediator.Send(new SearchProductsQuery(q ?? string.Empty), cancellationToken));
}
