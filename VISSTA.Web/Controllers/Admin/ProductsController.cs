using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Products;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/products")]
public sealed class ProductsController(IMediator mediator, IWebHostEnvironment environment) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var products = await mediator.Send(new GetProductListQuery(null, null, null, "newest", null), cancellationToken);
        return View("~/Views/Admin/Products/Index.cshtml", new AdminProductsViewModel(products));
    }

    [HttpGet("create")]
    public IActionResult Create() => View("~/Views/Admin/Products/Create.cshtml", new AdminProductFormViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Products/Create.cshtml", model);
        }

        try
        {
            var imageUrls = await SaveImagesAsync(model.ImageFiles, cancellationToken);
            await mediator.Send(new CreateProductCommand(model.Name, model.Slug, model.Description, model.Price, model.Stock, model.Sku, model.CategoryId, model.IsFeatured, imageUrls), cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "A product with the same slug or SKU already exists.");
        }

        return View("~/Views/Admin/Products/Create.cshtml", model);
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Products/Edit.cshtml", new AdminProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ExistingImages = product.Images,
            Price = product.Price,
            Stock = product.Stock,
            Sku = product.Sku,
            CategoryId = product.CategoryId,
            IsActive = true
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var product = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
            if (product is not null)
            {
                model.ExistingImages = product.Images;
            }
            return View("~/Views/Admin/Products/Edit.cshtml", model);
        }

        try
        {
            var imageUrls = await SaveImagesAsync(model.ImageFiles, cancellationToken);
            await mediator.Send(new UpdateProductCommand(id, model.Name, model.Slug, model.Description, model.Price, model.CategoryId, model.IsActive, model.IsFeatured, imageUrls, model.RemoveImageIds ?? Array.Empty<int>()), cancellationToken);
            await mediator.Send(new UpdateStockCommand(id, model.Stock), cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "A product with the same slug or SKU already exists.");
        }

        var existingProduct = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        if (existingProduct is not null)
        {
            model.ExistingImages = existingProduct.Images;
        }

        return View("~/Views/Admin/Products/Edit.cshtml", model);
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteProductCommand(id), cancellationToken);
        if (!deleted)
        {
            TempData["ProductAdminMessage"] = "Product was not found.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyCollection<string>> SaveImagesAsync(IFormFile[]? files, CancellationToken cancellationToken)
    {
        if (files is null || files.Length == 0)
        {
            return Array.Empty<string>();
        }

        var uploadsRoot = Path.Combine(environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var urls = new List<string>();
        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                continue;
            }

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsRoot, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);
            urls.Add($"/uploads/{fileName}");
        }

        return urls;
    }

    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
