using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Reviews;
using VISSTA.Application.Interfaces;

namespace VISSTA.Web.Controllers;

[Authorize]
public sealed class ReviewController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int productId, string slug, int rating, string body, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Challenge();
        }

        await mediator.Send(new SubmitReviewCommand(productId, currentUser.UserId, rating, body), cancellationToken);
        return RedirectToAction("Detail", "Shop", new { slug });
    }
}
