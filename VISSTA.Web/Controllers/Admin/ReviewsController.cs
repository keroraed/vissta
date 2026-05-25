using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VISSTA.Application.Features.Reviews;
using VISSTA.Infrastructure.Identity;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/reviews")]
public sealed class ReviewsController(IMediator mediator, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var reviews = await mediator.Send(new GetReviewListQuery(), cancellationToken);
        var customerIds = reviews
            .Select(x => x.CustomerId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArray();

        var emailLookup = customerIds.Length == 0
            ? new Dictionary<string, string>()
            : await userManager.Users
                .Where(user => customerIds.Contains(user.Id))
                .Select(user => new { user.Id, user.Email })
                .ToDictionaryAsync(user => user.Id, user => user.Email ?? string.Empty, cancellationToken);

        var items = reviews
            .Select(review => new AdminReviewItemViewModel(
                review,
                emailLookup.TryGetValue(review.CustomerId, out var email) ? email : string.Empty))
            .ToList();

        return View("~/Views/Admin/Reviews/Index.cshtml", new AdminReviewsViewModel(items));
    }

    [HttpPost("{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        await mediator.Send(new ApproveReviewCommand(id), cancellationToken);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
