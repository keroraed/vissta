using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VISSTA.Application.Features.Auth.Commands.RequestPasswordReset;
using VISSTA.Application.Features.Auth.Commands.ResetPasswordOtp;
using VISSTA.Application.Features.Auth.Commands.VerifyOtp;
using VISSTA.Application.Features.Orders;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Domain.ValueObjects;
using VISSTA.Infrastructure.Identity;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers;

public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    IRepository<Customer> customers,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ICurrentUserService currentUser) : Controller
{
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is not null && await userManager.IsInRoleAsync(user, "Admin"))
        {
            return LocalRedirect("/admin");
        }

        return LocalRedirect(model.ReturnUrl ?? "/");
    }

    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, PhoneNumber = model.PhoneNumber };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                if (string.Equals(error.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        if (!await roleManager.RoleExistsAsync("Customer"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole("Customer"));
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        var roleAssignResult = await userManager.AddToRoleAsync(user, "Customer");
        if (!roleAssignResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            foreach (var error in roleAssignResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var address = new Address(model.Street, model.City, model.Governorate, model.PostalCode, model.Country);
        await customers.AddAsync(new Customer(user.Id, model.FullName, model.PhoneNumber, user.Email ?? string.Empty, address), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var customer = await customers.Query()
            .FirstOrDefaultAsync(x => x.Id == user.Id, cancellationToken);

        return View(BuildProfileViewModel(user, customer));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var address = new Address(model.Street, model.City, model.Governorate, model.PostalCode, model.Country);
        var customer = await customers.Query()
            .FirstOrDefaultAsync(x => x.Id == user.Id, cancellationToken);

        if (customer is null)
        {
            await customers.AddAsync(new Customer(user.Id, model.FullName, model.PhoneNumber, user.Email ?? string.Empty, address), cancellationToken);
        }
        else
        {
            customer.UpdateProfile(model.FullName, model.PhoneNumber, user.Email ?? string.Empty, address);
            customers.Update(customer);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    public async Task<IActionResult> Orders(CancellationToken cancellationToken)
    {
        var orders = await mediator.Send(new GetOrderHistoryQuery(currentUser.UserId ?? string.Empty), cancellationToken);
        return View(new AccountOrdersViewModel(orders));
    }

    [Authorize]
    [HttpGet("Account/Orders/{id:int}")]
    public async Task<IActionResult> Order(int id, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Challenge();
        }

        var order = await mediator.Send(new GetOrderByIdQuery(id, currentUser.UserId), cancellationToken);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    // ─────────────────────────────────────────────────────────────────────────
    // OTP-BASED FORGOT PASSWORD FLOW
    // ─────────────────────────────────────────────────────────────────────────

    // STEP 1 — Show email form
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    // STEP 1 — Submit email → dispatch RequestPasswordResetCommand
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        try
        {
            await mediator.Send(new RequestPasswordResetCommand(vm.Email));
            TempData["ResetEmail"] = vm.Email;
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(vm);
        }
    }

    // STEP 1 — Confirmation: "email sent" message
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        var email = TempData["ResetEmail"] as string;
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        // Keep email available for the "Enter Code" button
        TempData.Keep("ResetEmail");

        return View(new ForgotPasswordConfirmationViewModel { Email = email, MaskedEmail = MaskEmail(email) });
    }

    // STEP 2 — Show OTP entry form
    [HttpGet]
    public IActionResult VerifyOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            email = (TempData.Peek("ResetEmail") as string) ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        TempData.Keep("ResetEmail");
        return View(new VerifyOtpViewModel { Email = email });
    }

    // STEP 2 — Submit OTP
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        try
        {
            var result = await mediator.Send(new VerifyOtpCommand(vm.Email, vm.Otp));
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid code.");
                return View(vm);
            }

            TempData["OtpId"] = result.OtpId.ToString();
            TempData["ResetEmail"] = vm.Email;
            return RedirectToAction(nameof(ResetPassword), new { email = vm.Email, otpId = result.OtpId });
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(vm);
        }
    }

    // STEP 3 — Show new password form
    [HttpGet]
    public IActionResult ResetPassword(string email, Guid otpId)
    {
        if (string.IsNullOrWhiteSpace(email) || otpId == Guid.Empty)
        {
            var otpIdStr = TempData.Peek("OtpId") as string;
            var emailStr = TempData.Peek("ResetEmail") as string;

            if (string.IsNullOrWhiteSpace(otpIdStr) || string.IsNullOrWhiteSpace(emailStr)
                || !Guid.TryParse(otpIdStr, out otpId))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }
            email = emailStr ?? string.Empty;
        }

        TempData.Keep("OtpId");
        TempData.Keep("ResetEmail");

        return View(new ResetPasswordViewModel { OtpId = otpId, Email = email });
    }

    // STEP 3 — Submit new password
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        try
        {
            var result = await mediator.Send(new ResetPasswordOtpCommand(
                vm.OtpId, vm.Email, vm.NewPassword, vm.ConfirmPassword));

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An error occurred.");
                return View(vm);
            }

            TempData["SuccessMessage"] = "Password updated successfully.";
            return RedirectToAction(nameof(ResetPasswordSuccess));
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(vm);
        }
    }

    // SUCCESS PAGE
    [HttpGet]
    public IActionResult ResetPasswordSuccess() => View();

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private static ProfileViewModel BuildProfileViewModel(ApplicationUser user, Customer? customer)
    {
        var address = customer?.DefaultAddress;
        return new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? customer?.PhoneNumber ?? string.Empty,
            Street = address?.Street ?? string.Empty,
            City = address?.City ?? string.Empty,
            Governorate = address?.Governorate ?? string.Empty,
            PostalCode = address?.PostalCode ?? string.Empty,
            Country = address?.Country ?? "Egypt"
        };
    }

    /// <summary>Masks the middle portion of an email: "l****a@gmail.com"</summary>
    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return email;

        var local = email[..atIndex];
        var domain = email[atIndex..];

        if (local.Length <= 2)
        {
            return $"{local[0]}*{domain}";
        }

        var visible = Math.Max(1, local.Length / 4);
        var masked = local[..1] + new string('*', local.Length - visible - 1) + local[^1..];
        return masked + domain;
    }
}
