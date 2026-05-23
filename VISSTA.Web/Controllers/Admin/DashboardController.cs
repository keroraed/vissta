using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Coupons;
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
        var products = await mediator.Send(new GetProductListQuery(null, null, null, null, null, true), cancellationToken);
        var coupons = await mediator.Send(new GetCouponsQuery(), cancellationToken);
        var discounts = orders.Sum(x => x.DiscountAmount);
        var model = new AdminDashboardViewModel(
            orders.Sum(x => x.TotalAmount),
            orders.Sum(x => x.TotalAmount + x.DiscountAmount),
            discounts,
            orders.Count == 0 ? 0 : orders.Average(x => x.TotalAmount),
            orders.Count,
            orders.Count(x => x.Status is "Pending" or "Confirmed"),
            products.Count(x => x.Stock <= 5),
            coupons.Count(x => x.IsValid),
            orders.Take(6).ToList(),
            products.OrderByDescending(x => x.IsFeatured).ThenByDescending(x => x.Stock).Take(5).ToList(),
            products.Where(x => x.Stock <= 5).OrderBy(x => x.Stock).Take(5).ToList());

        return View("~/Views/Admin/Dashboard/Index.cshtml", model);
    }
}
