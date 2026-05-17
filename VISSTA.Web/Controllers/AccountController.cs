using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Orders;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Infrastructure.Identity;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers;

public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
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

        await userManager.AddToRoleAsync(user, "Customer");
        await customers.AddAsync(new Customer(user.Id, model.FullName, model.PhoneNumber), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User);
        return View(new ProfileViewModel(user?.FullName ?? string.Empty, user?.Email ?? string.Empty, user?.PhoneNumber ?? string.Empty));
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
}
