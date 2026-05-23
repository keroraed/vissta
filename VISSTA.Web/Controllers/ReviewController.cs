using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Reviews;
using VISSTA.Application.Interfaces;
using VISSTA.Infrastructure.Identity;

namespace VISSTA.Web.Controllers;

public sealed class ReviewController(IMediator mediator, ICurrentUserService currentUser, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int productId, string slug, int rating, string body, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            TempData["Message"] = "Please sign in to submit your review.";
            var returnUrl = Url.Action("Detail", "Shop", new { slug }) ?? "/";
            return RedirectToAction("Login", "Account", new { returnUrl = $"{returnUrl}#reviews" });
        }

        var user = await userManager.GetUserAsync(User);
        var customerName = user?.FullName;
        if (string.IsNullOrWhiteSpace(customerName))
        {
            customerName = User.Identity?.Name ?? "VISSTA Customer";
        }

        await mediator.Send(new SubmitReviewCommand(productId, currentUser.UserId, customerName, user?.PhoneNumber ?? string.Empty, rating, body), cancellationToken);
        TempData["Message"] = "Review submitted successfully. It will appear after approval.";
        return RedirectToAction("Detail", "Shop", new { slug });
    }
}
