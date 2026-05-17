using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Orders;
using VISSTA.Domain.Enums;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/orders")]
public sealed class OrdersController(IMediator mediator) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(OrderStatus? status, CancellationToken cancellationToken)
    {
        var orders = await mediator.Send(new GetAllOrdersQuery(status), cancellationToken);
        return View("~/Views/Admin/Orders/Index.cshtml", new AdminOrdersViewModel(orders, status));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var order = await mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return order is null ? NotFound() : View("~/Views/Admin/Orders/Detail.cshtml", new AdminOrderDetailViewModel(order));
    }

    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateOrderStatusCommand(id, status), cancellationToken);
        return RedirectToAction(nameof(Detail), new { id });
    }
}
