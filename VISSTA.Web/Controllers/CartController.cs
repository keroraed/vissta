using MediatR;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Cart;
using VISSTA.Application.Interfaces;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers;

public sealed class CartController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var cart = await mediator.Send(new GetCartQuery(currentUser.UserId, currentUser.SessionId), cancellationToken);
        return View(new CartViewModel(cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity, CancellationToken cancellationToken)
    {
        var cart = await mediator.Send(new AddToCartCommand(currentUser.UserId, currentUser.SessionId, productId, quantity), cancellationToken);
        return Request.Headers.XRequestedWith == "XMLHttpRequest" ? Json(cart) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId, CancellationToken cancellationToken)
    {
        var cart = await mediator.Send(new RemoveFromCartCommand(currentUser.UserId, currentUser.SessionId, cartItemId), cancellationToken);
        return Request.Headers.XRequestedWith == "XMLHttpRequest" ? Json(cart) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int cartItemId, int quantity, CancellationToken cancellationToken)
    {
        var cart = await mediator.Send(new UpdateCartItemCommand(currentUser.UserId, currentUser.SessionId, cartItemId, quantity), cancellationToken);
        return Request.Headers.XRequestedWith == "XMLHttpRequest" ? Json(cart) : RedirectToAction(nameof(Index));
    }

    [HttpPost("/api/cart/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApiAdd([FromForm] int productId, [FromForm] int quantity, CancellationToken cancellationToken) =>
        Json(await mediator.Send(new AddToCartCommand(currentUser.UserId, currentUser.SessionId, productId, quantity), cancellationToken));

    [HttpPost("/api/cart/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApiRemove([FromForm] int cartItemId, CancellationToken cancellationToken) =>
        Json(await mediator.Send(new RemoveFromCartCommand(currentUser.UserId, currentUser.SessionId, cartItemId), cancellationToken));

    [HttpPut("/api/cart/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApiUpdate([FromForm] int cartItemId, [FromForm] int quantity, CancellationToken cancellationToken) =>
        Json(await mediator.Send(new UpdateCartItemCommand(currentUser.UserId, currentUser.SessionId, cartItemId, quantity), cancellationToken));

    [HttpGet("/api/cart/count")]
    public async Task<IActionResult> Count(CancellationToken cancellationToken)
    {
        var cart = await mediator.Send(new GetCartQuery(currentUser.UserId, currentUser.SessionId), cancellationToken);
        return Json(new { count = cart.Count });
    }
}
