using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;
using VISSTA.Application.DTOs;
using VISSTA.Application.Features.Categories;
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
        var products = await mediator.Send(new GetProductListQuery(null, null, null, "newest", null, true), cancellationToken);
        return View("~/Views/Admin/Products/Index.cshtml", new AdminProductsViewModel(products));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var categories = await LoadCategoriesAsync(cancellationToken);
        return View("~/Views/Admin/Products/Create.cshtml", new AdminProductFormViewModel { Categories = categories });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminProductFormViewModel model, CancellationToken cancellationToken)
    {
        model.Categories = await LoadCategoriesAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = Slugify(model.Name);
        }
        if (string.IsNullOrWhiteSpace(model.Sku))
        {
            model.Sku = GenerateSku(model.Slug);
        }
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Products/Create.cshtml", model);
        }

        try
        {
            var imageUrls = await SaveImagesAsync(model.ImageFiles, cancellationToken);
            await mediator.Send(new CreateProductCommand(model.Name, model.Slug, model.Description, model.Price, model.StockS, model.StockM, model.StockL, model.StockXL, model.Sku, model.CategoryId, model.IsFeatured, imageUrls), cancellationToken);
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

        var categories = await LoadCategoriesAsync(cancellationToken);

        return View("~/Views/Admin/Products/Edit.cshtml", new AdminProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ExistingImages = product.Images,
            Price = product.Price,
            Stock = product.Stock,
            StockS = product.SizeStocks.FirstOrDefault(x => x.Size == "S")?.Stock ?? 0,
            StockM = product.SizeStocks.FirstOrDefault(x => x.Size == "M")?.Stock ?? 0,
            StockL = product.SizeStocks.FirstOrDefault(x => x.Size == "L")?.Stock ?? 0,
            StockXL = product.SizeStocks.FirstOrDefault(x => x.Size == "XL")?.Stock ?? 0,
            Sku = product.Sku,
            CategoryId = product.CategoryId,
            Categories = categories,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminProductFormViewModel model, CancellationToken cancellationToken)
    {
        model.Categories = await LoadCategoriesAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            var product = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
            if (product is not null)
            {
                model.ExistingImages = product.Images;
                model.Sku = product.Sku;
                model.IsActive = product.IsActive;
                model.IsFeatured = product.IsFeatured;
            }
            return View("~/Views/Admin/Products/Edit.cshtml", model);
        }

        try
        {
            var imageUrls = await SaveImagesAsync(model.ImageFiles, cancellationToken);
            await mediator.Send(new UpdateProductCommand(id, model.Name, model.Slug, model.Description, model.Price, model.CategoryId, model.IsActive, model.IsFeatured, imageUrls, model.RemoveImageIds ?? Array.Empty<int>()), cancellationToken);
            await mediator.Send(new UpdateStockCommand(id, model.StockS, model.StockM, model.StockL, model.StockXL), cancellationToken);
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
            model.Sku = existingProduct.Sku;
            model.IsActive = existingProduct.IsActive;
            model.IsFeatured = existingProduct.IsFeatured;
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

    private Task<IReadOnlyCollection<CategoryDto>> LoadCategoriesAsync(CancellationToken cancellationToken) =>
        mediator.Send(new GetCategoryListQuery(), cancellationToken);

    private static string Slugify(string value)
    {
        var cleaned = Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9\s-]", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        cleaned = cleaned.Replace(" ", "-");
        return cleaned.Length == 0 ? "product" : cleaned;
    }

    private static string GenerateSku(string slug)
    {
        var baseSku = $"VIS-{slug.ToUpperInvariant()}";
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var sku = $"{baseSku}-{suffix}";
        return sku.Length <= 64 ? sku : sku[^64..];
    }
}
