using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using VISSTA.Application.Features.Categories;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/categories")]
public sealed class CategoriesController(IMediator mediator) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await mediator.Send(new GetCategoryListQuery(), cancellationToken);
        return View("~/Views/Admin/Categories/Index.cshtml", new AdminCategoriesViewModel(categories));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var categories = await mediator.Send(new GetCategoryListQuery(), cancellationToken);
        return View("~/Views/Admin/Categories/Create.cshtml", new AdminCategoryFormViewModel { Categories = categories });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCategoryFormViewModel model, CancellationToken cancellationToken)
    {
        model.Categories = await mediator.Send(new GetCategoryListQuery(), cancellationToken);
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = Slugify(model.Name);
        }

        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Categories/Create.cshtml", model);
        }

        try
        {
            await mediator.Send(new CreateCategoryCommand(model.Name, model.Slug, model.ParentCategoryId), cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "A category with the same slug already exists.");
        }

        return View("~/Views/Admin/Categories/Create.cshtml", model);
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = await mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var categories = await mediator.Send(new GetCategoryListQuery(), cancellationToken);
        return View("~/Views/Admin/Categories/Edit.cshtml", new AdminCategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            ParentCategoryId = category.ParentCategoryId,
            Categories = categories.Where(c => c.Id != category.Id).ToList()
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminCategoryFormViewModel model, CancellationToken cancellationToken)
    {
        model.Categories = await mediator.Send(new GetCategoryListQuery(), cancellationToken);
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = Slugify(model.Name);
        }

        if (model.ParentCategoryId == id)
        {
            ModelState.AddModelError(nameof(model.ParentCategoryId), "A category cannot be its own parent.");
        }

        if (!ModelState.IsValid)
        {
            model.Categories = model.Categories.Where(c => c.Id != id).ToList();
            return View("~/Views/Admin/Categories/Edit.cshtml", model);
        }

        try
        {
            var updated = await mediator.Send(new UpdateCategoryCommand(id, model.Name, model.Slug, model.ParentCategoryId), cancellationToken);
            if (!updated)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "A category with the same slug already exists.");
        }

        model.Categories = model.Categories.Where(c => c.Id != id).ToList();
        return View("~/Views/Admin/Categories/Edit.cshtml", model);
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        if (!deleted)
        {
            TempData["CategoryAdminMessage"] = "Category could not be deleted. Remove products from it first.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static string Slugify(string value)
    {
        var cleaned = Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9\s-]", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        cleaned = cleaned.Replace(" ", "-");
        return cleaned.Length == 0 ? "category" : cleaned;
    }

    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
