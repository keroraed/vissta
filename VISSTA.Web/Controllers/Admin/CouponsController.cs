using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Coupons;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/coupons")]
public sealed class CouponsController(IMediator mediator) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var coupons = await mediator.Send(new GetCouponsQuery(), cancellationToken);
        return View("~/Views/Admin/Coupons/Index.cshtml", new AdminCouponsViewModel(coupons));
    }

    [HttpGet("create")]
    public IActionResult Create() => View("~/Views/Admin/Coupons/Create.cshtml", new AdminCouponFormViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCouponFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Coupons/Create.cshtml", model);
        }

        await mediator.Send(new CreateCouponCommand(model.Code, model.DiscountType, model.Value, model.ExpiryDate, model.MaxUses, model.IsActive), cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var coupon = await mediator.Send(new GetCouponByIdQuery(id), cancellationToken);
        if (coupon is null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Coupons/Edit.cshtml", new AdminCouponFormViewModel
        {
            Id = coupon.Id,
            Code = coupon.Code,
            DiscountType = Enum.Parse<VISSTA.Domain.Enums.DiscountType>(coupon.DiscountType),
            Value = coupon.Value,
            ExpiryDate = coupon.ExpiryDate,
            MaxUses = coupon.MaxUses,
            IsActive = coupon.IsActive
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminCouponFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Coupons/Edit.cshtml", model);
        }

        await mediator.Send(new UpdateCouponCommand(id, model.Code, model.DiscountType, model.Value, model.ExpiryDate, model.MaxUses, model.IsActive), cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteCouponCommand(id), cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
