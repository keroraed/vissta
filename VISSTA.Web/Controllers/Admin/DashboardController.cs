using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Orders;
using VISSTA.Application.Features.Products;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin")]
public sealed class DashboardController(IMediator mediator) : Controller
{
    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await mediator.Send(new GetAllOrdersQuery(), cancellationToken);
        var products = await mediator.Send(new GetProductListQuery(null, null, null, null, null), cancellationToken);
        var model = new AdminDashboardViewModel(
            orders.Sum(x => x.TotalAmount),
            orders.Count,
            products.Count(x => x.Stock <= 5),
            orders.Take(6).ToList(),
            products.OrderByDescending(x => x.IsFeatured).Take(5).ToList());

        return View("~/Views/Admin/Dashboard/Index.cshtml", model);
    }
}
