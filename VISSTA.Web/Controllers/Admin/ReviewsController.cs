using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Reviews;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/reviews")]
public sealed class ReviewsController(IMediator mediator) : Controller
{
    [HttpPost("{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new ApproveReviewCommand(id), cancellationToken);
        return RedirectToAction("Index", "Dashboard");
    }
}
