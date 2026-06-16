using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Sizes;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/sizes")]
public sealed class SizesController(IMediator mediator) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var sizes = await mediator.Send(new GetSizesQuery(), cancellationToken);
        return View("~/Views/Admin/Sizes/Index.cshtml", new AdminSizesViewModel(sizes));
    }

    [HttpGet("create")]
    public IActionResult Create() => View("~/Views/Admin/Sizes/Create.cshtml", new AdminSizeFormViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminSizeFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Sizes/Create.cshtml", model);
        }

        try
        {
            await mediator.Send(new CreateSizeCommand(model.Name, model.DisplayOrder), cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.Name), ex.Message);
            return View("~/Views/Admin/Sizes/Create.cshtml", model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var size = await mediator.Send(new GetSizeByIdQuery(id), cancellationToken);
        if (size is null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Sizes/Edit.cshtml", new AdminSizeFormViewModel
        {
            Id = size.Id,
            Name = size.Name,
            DisplayOrder = size.DisplayOrder
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminSizeFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Sizes/Edit.cshtml", model);
        }

        try
        {
            var updated = await mediator.Send(new UpdateSizeCommand(id, model.Name, model.DisplayOrder), cancellationToken);
            if (!updated)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.Name), ex.Message);
            return View("~/Views/Admin/Sizes/Edit.cshtml", model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteSizeCommand(id), cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
