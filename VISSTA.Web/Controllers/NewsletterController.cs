using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Newsletter;

namespace VISSTA.Web.Controllers;

public sealed class NewsletterController(IMediator mediator) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Newsletter/Subscribe")]
    public async Task<IActionResult> Subscribe(string email, CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator.Send(new SubscribeNewsletterCommand(email), cancellationToken);
            return Json(new { success = result.Success, message = result.Message });
        }
        catch (ValidationException ex)
        {
            var message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Please enter a valid email address.";
            return Json(new { success = false, message });
        }
    }

    [HttpGet]
    [Route("Newsletter/Unsubscribe")]
    public async Task<IActionResult> Unsubscribe(string token, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnsubscribeNewsletterCommand(token), cancellationToken);
        return View("UnsubscribeResult", result);
    }
}
