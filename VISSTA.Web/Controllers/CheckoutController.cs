using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Cart;
using VISSTA.Application.Features.Orders;
using VISSTA.Application.Interfaces;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers;

[Authorize]
public sealed class CheckoutController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var cart = await mediator.Send(new GetCartQuery(currentUser.UserId, currentUser.SessionId), cancellationToken);
        return View(new CheckoutViewModel { Cart = cart });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(CheckoutViewModel model, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Challenge();
        }

        var orderId = await mediator.Send(new PlaceOrderCommand(
            currentUser.UserId,
            currentUser.SessionId,
            model.ShippingAddress.Street,
            model.ShippingAddress.City,
            model.ShippingAddress.Governorate,
            model.ShippingAddress.PostalCode,
            model.ShippingAddress.Country,
            model.PaymentToken,
            model.CouponCode), cancellationToken);

        return RedirectToAction(nameof(Success), new { id = orderId });
    }

    public async Task<IActionResult> Success(int id, CancellationToken cancellationToken)
    {
        var order = await mediator.Send(new GetOrderByIdQuery(id, currentUser.UserId), cancellationToken);
        return View(new OrderConfirmationViewModel(id, order));
    }

    public IActionResult Failed() => View();
}
