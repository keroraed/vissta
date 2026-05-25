using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VISSTA.Application.Features.Coupons;
using VISSTA.Application.Features.Orders;
using VISSTA.Application.Features.Products;
using VISSTA.Application.Features.Reviews;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin")]
public sealed class DashboardController(IMediator mediator, IRepository<AppSetting> settings) : Controller
{
    private const string LowStockKey = "LowStockThreshold";
    private const int DefaultThreshold = 5;

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var thresholdValue = await settings.QueryReadOnly()
            .Where(x => x.Key == LowStockKey)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);
        var threshold = int.TryParse(thresholdValue, out var parsed) ? parsed : DefaultThreshold;
        var orders = await mediator.Send(new GetAllOrdersQuery(), cancellationToken);
        var products = await mediator.Send(new GetProductListQuery(null, null, null, null, null, true), cancellationToken);
        var coupons = await mediator.Send(new GetCouponsQuery(), cancellationToken);
        var reviews = await mediator.Send(new GetRecentReviewsQuery(6), cancellationToken);
        var discounts = orders.Sum(x => x.DiscountAmount);
        var model = new AdminDashboardViewModel(
            orders.Sum(x => x.TotalAmount),
            orders.Sum(x => x.TotalAmount + x.DiscountAmount),
            discounts,
            orders.Count == 0 ? 0 : orders.Average(x => x.TotalAmount),
            orders.Count,
            orders.Count(x => x.Status is "Pending" or "Confirmed"),
            products.Count(x => x.Stock <= threshold),
            coupons.Count(x => x.IsValid),
            orders.Take(6).ToList(),
            products.OrderByDescending(x => x.IsFeatured).ThenByDescending(x => x.Stock).Take(5).ToList(),
            products.Where(x => x.Stock <= threshold).OrderBy(x => x.Stock).Take(5).ToList(),
            reviews);

        return View("~/Views/Admin/Dashboard/Index.cshtml", model);
    }
}
