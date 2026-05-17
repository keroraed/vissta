using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe == true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
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
        await customers.AddAsync(new Customer(user.Id, model.FullName, model.PhoneNumber, address), cancellationToken);
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
            await customers.AddAsync(new Customer(user.Id, model.FullName, model.PhoneNumber, address), cancellationToken);
        }
        else
        {
            customer.UpdateProfile(model.FullName, model.PhoneNumber, address);
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

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

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
}
