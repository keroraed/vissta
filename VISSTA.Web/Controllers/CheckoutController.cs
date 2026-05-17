using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VISSTA.Application.DTOs;
using VISSTA.Application.Features.Cart;
using VISSTA.Application.Features.Orders;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Domain.ValueObjects;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers;

[Authorize]
public sealed class CheckoutController(
    IMediator mediator,
    ICurrentUserService currentUser,
    IRepository<Customer> customers) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var cart = await mediator.Send(new GetCartQuery(currentUser.UserId, currentUser.SessionId), cancellationToken);
        if (cart.Count == 0)
        {
            TempData["Message"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        return View(await BuildCheckoutModelAsync(cart, null, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(CheckoutViewModel model, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Challenge();
        }

        var cart = await mediator.Send(new GetCartQuery(currentUser.UserId, currentUser.SessionId), cancellationToken);
        if (cart.Count == 0)
        {
            TempData["Message"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        var shippingAddress = model.UseSavedAddress
            ? await GetSavedAddressAsync(currentUser.UserId, cancellationToken)
            : ToAddress(model.ShippingAddress);

        if (shippingAddress is null || !IsComplete(shippingAddress))
        {
            ModelState.AddModelError(string.Empty, "Choose your saved address or enter a complete shipping address.");
            return View(await BuildCheckoutModelAsync(cart, model, cancellationToken));
        }

        var orderId = await mediator.Send(new PlaceOrderCommand(
            currentUser.UserId,
            currentUser.SessionId,
            shippingAddress.Street,
            shippingAddress.City,
            shippingAddress.Governorate,
            shippingAddress.PostalCode,
            shippingAddress.Country,
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

    private async Task<CheckoutViewModel> BuildCheckoutModelAsync(CartDto cart, CheckoutViewModel? posted, CancellationToken cancellationToken)
    {
        var model = posted ?? new CheckoutViewModel();
        model.Cart = cart;

        if (currentUser.UserId is null)
        {
            return model;
        }

        var savedAddress = await GetSavedAddressAsync(currentUser.UserId, cancellationToken);
        if (savedAddress is not null)
        {
            model.SavedAddress = ToInput(savedAddress);
            if (posted is null)
            {
                model.UseSavedAddress = true;
            }
        }

        return model;
    }

    private async Task<Address?> GetSavedAddressAsync(string userId, CancellationToken cancellationToken)
    {
        var customer = await customers.QueryReadOnly()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        return customer?.DefaultAddress;
    }

    private static ShippingAddressInput ToInput(Address address) => new()
    {
        Street = address.Street,
        City = address.City,
        Governorate = address.Governorate,
        PostalCode = address.PostalCode,
        Country = address.Country
    };

    private static Address ToAddress(ShippingAddressInput input) =>
        new(input.Street, input.City, input.Governorate, input.PostalCode, input.Country);

    private static bool IsComplete(Address address) =>
        !string.IsNullOrWhiteSpace(address.Street)
        && !string.IsNullOrWhiteSpace(address.City)
        && !string.IsNullOrWhiteSpace(address.Governorate)
        && !string.IsNullOrWhiteSpace(address.Country);
}
