using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VISSTA.Application.DTOs;
using VISSTA.Application.Features.Cart;
using VISSTA.Application.Features.Orders;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Domain.Enums;
using VISSTA.Domain.ValueObjects;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers;

public sealed class CheckoutController(
    IMediator mediator,
    ICurrentUserService currentUser,
    IRepository<Customer> customers,
    IRepository<Coupon> coupons,
    IRepository<AppSetting> settings,
    IFileStorageService fileStorage) : Controller
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
        var customerId = currentUser.UserId ?? $"guest:{currentUser.SessionId}";
        if (!currentUser.IsAuthenticated)
        {
            if (string.IsNullOrWhiteSpace(model.GuestName))
            {
                ModelState.AddModelError(nameof(model.GuestName), "Name is required for guest checkout.");
            }

            var emailValidator = new EmailAddressAttribute();
            if (string.IsNullOrWhiteSpace(model.GuestEmail) || !emailValidator.IsValid(model.GuestEmail))
            {
                ModelState.AddModelError(nameof(model.GuestEmail), "A valid email is required for guest checkout.");
            }

            var phoneValidator = new PhoneAttribute();
            if (string.IsNullOrWhiteSpace(model.GuestPhone) || !phoneValidator.IsValid(model.GuestPhone))
            {
                ModelState.AddModelError(nameof(model.GuestPhone), "A valid phone number is required for guest checkout.");
            }
        }

        if (model.PaymentMethod == PaymentMethod.InstaPayWallet)
        {
            if (model.PaymentProof is null || model.PaymentProof.Length == 0)
            {
                ModelState.AddModelError(nameof(model.PaymentProof), "Please upload a payment proof.");
            }
            else if (!IsAllowedProof(model.PaymentProof, out var proofError))
            {
                ModelState.AddModelError(nameof(model.PaymentProof), proofError);
            }
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            Console.WriteLine($"[CheckoutController] ModelState is invalid: {errors}");
            TempData["Message"] = $"Validation failed: {errors}";
            var cartModel = await mediator.Send(new GetCartQuery(currentUser.UserId, currentUser.SessionId), cancellationToken);
            return View("Index", await BuildCheckoutModelAsync(cartModel, model, cancellationToken));
        }

        var cart = await mediator.Send(new GetCartQuery(currentUser.UserId, currentUser.SessionId), cancellationToken);
        if (cart.Count == 0)
        {
            Console.WriteLine("[CheckoutController] Cart is empty.");
            TempData["Message"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        var shippingAddress = model.UseSavedAddress && currentUser.UserId is not null
            ? await GetSavedAddressAsync(currentUser.UserId, cancellationToken)
            : ToAddress(model.ShippingAddress);

        if (shippingAddress is null || !IsComplete(shippingAddress))
        {
            Console.WriteLine("[CheckoutController] Incomplete shipping address.");
            TempData["Message"] = "Incomplete shipping address. Please select or enter a complete address.";
            ModelState.AddModelError(string.Empty, "Choose your saved address or enter a complete shipping address.");
            return View("Index", await BuildCheckoutModelAsync(cart, model, cancellationToken));
        }

        int orderId;
        try
        {
            string? proofUrl = null;
            if (model.PaymentMethod == PaymentMethod.InstaPayWallet && model.PaymentProof is not null && model.PaymentProof.Length > 0)
            {
                proofUrl = await fileStorage.SaveAsync(model.PaymentProof.OpenReadStream(), model.PaymentProof.FileName, model.PaymentProof.ContentType, cancellationToken);
            }

            orderId = await mediator.Send(new PlaceOrderCommand(
                customerId,
                currentUser.SessionId,
                shippingAddress.Street,
                shippingAddress.City,
                shippingAddress.Governorate,
                shippingAddress.PostalCode,
                shippingAddress.Country,
                model.PaymentMethod,
                proofUrl,
                model.PaymentToken,
                model.CouponCode,
                currentUser.IsAuthenticated ? null : model.GuestName,
                currentUser.IsAuthenticated ? (User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.Identity?.Name) : model.GuestEmail,
                currentUser.IsAuthenticated ? null : model.GuestPhone), cancellationToken);
        }
        catch (FluentValidation.ValidationException ex)
        {
            var errors = string.Join(", ", ex.Errors.Select(e => e.ErrorMessage));
            Console.WriteLine($"[CheckoutController] FluentValidation exception: {errors}");
            TempData["Message"] = $"Validation failed: {errors}";
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return View("Index", await BuildCheckoutModelAsync(cart, model, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[CheckoutController] InvalidOperationException: {ex.Message}");
            TempData["Message"] = $"Order failed: {ex.Message}";
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Index", await BuildCheckoutModelAsync(cart, model, cancellationToken));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CheckoutController] Unexpected exception: {ex.Message}\n{ex.StackTrace}");
            TempData["Message"] = $"Unexpected error: {ex.Message}";
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Index", await BuildCheckoutModelAsync(cart, model, cancellationToken));
        }

        return RedirectToAction(nameof(Success), new { id = orderId });
    }

    public async Task<IActionResult> Success(int id, CancellationToken cancellationToken)
    {
        var customerId = currentUser.UserId ?? $"guest:{currentUser.SessionId}";
        var order = await mediator.Send(new GetOrderByIdQuery(id, customerId), cancellationToken);
        return View(new OrderConfirmationViewModel(id, order));
    }

    public IActionResult Failed() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateCoupon([FromForm] string? couponCode, CancellationToken cancellationToken)
    {
        var cart = await mediator.Send(new GetCartQuery(currentUser.UserId, currentUser.SessionId), cancellationToken);
        if (cart.Count == 0)
        {
            return Json(new
            {
                valid = false,
                discountAmount = 0m,
                totalAmount = cart.Subtotal,
                currency = cart.Currency,
                code = string.Empty
            });
        }

        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return Json(new
            {
                valid = false,
                discountAmount = 0m,
                totalAmount = cart.Subtotal,
                currency = cart.Currency,
                code = string.Empty
            });
        }

        var normalized = couponCode.Trim().ToUpperInvariant();
        var coupon = await coupons.QueryReadOnly()
            .FirstOrDefaultAsync(x => x.Code == normalized, cancellationToken);

        if (coupon is null || !coupon.IsValid(DateTime.UtcNow))
        {
            return Json(new
            {
                valid = false,
                discountAmount = 0m,
                totalAmount = cart.Subtotal,
                currency = cart.Currency,
                code = string.Empty
            });
        }

        var discount = coupon.CalculateDiscount(cart.Subtotal);
        var total = Math.Max(0, cart.Subtotal - discount);

        return Json(new
        {
            valid = true,
            discountAmount = discount,
            totalAmount = total,
            currency = cart.Currency,
            code = coupon.Code
        });
    }

    private async Task<CheckoutViewModel> BuildCheckoutModelAsync(CartDto cart, CheckoutViewModel? posted, CancellationToken cancellationToken)
    {
        var model = posted ?? new CheckoutViewModel();
        model.Cart = cart;
        await PopulatePaymentSettingsAsync(model, cancellationToken);

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

    private async Task PopulatePaymentSettingsAsync(CheckoutViewModel model, CancellationToken cancellationToken)
    {
        var values = await settings.QueryReadOnly()
            .Where(x => PaymentSettingKeys.ManualPaymentPhoneNumbers.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);

        model.InstaPayPhoneNumber = values.GetValueOrDefault(PaymentSettingKeys.InstaPayPhoneNumber);
        model.VodafoneCashPhoneNumber = values.GetValueOrDefault(PaymentSettingKeys.VodafoneCashPhoneNumber);
        model.OrangeCashPhoneNumber = values.GetValueOrDefault(PaymentSettingKeys.OrangeCashPhoneNumber);
        model.EtisalatCashPhoneNumber = values.GetValueOrDefault(PaymentSettingKeys.EtisalatCashPhoneNumber);
        model.WePayPhoneNumber = values.GetValueOrDefault(PaymentSettingKeys.WePayPhoneNumber);
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
        new(
            input.Street ?? string.Empty,
            input.City ?? string.Empty,
            input.Governorate ?? string.Empty,
            input.PostalCode ?? string.Empty,
            input.Country ?? "Egypt");

    private static bool IsComplete(Address address) =>
        !string.IsNullOrWhiteSpace(address.Street)
        && !string.IsNullOrWhiteSpace(address.City)
        && !string.IsNullOrWhiteSpace(address.Governorate)
        && !string.IsNullOrWhiteSpace(address.Country);

    private static bool IsAllowedProof(IFormFile file, out string error)
    {
        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
        {
            error = "Payment proof must be 5MB or less.";
            return false;
        }

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "application/pdf" };
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            error = "Payment proof must be an image or PDF.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
