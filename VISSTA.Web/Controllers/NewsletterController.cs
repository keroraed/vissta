using MediatR;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Newsletter;

namespace VISSTA.Web.Controllers;

public sealed class NewsletterController(IMediator mediator) : Controller
{
    [HttpPost("/api/newsletter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(string email, CancellationToken cancellationToken)
    {
        await mediator.Send(new SubscribeNewsletterCommand(email), cancellationToken);
        return Json(new { ok = true });
    }
}
