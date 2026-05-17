using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Application.Features.Products;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/products")]
public sealed class ProductsController(IMediator mediator) : Controller
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

        await mediator.Send(new CreateProductCommand(model.Name, model.Slug, model.Description, model.Price, model.Stock, model.Sku, model.CategoryId, model.IsFeatured), cancellationToken);
        return RedirectToAction(nameof(Index));
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
            Price = product.Price,
            Stock = product.Stock,
            Sku = product.Sku,
            IsActive = true
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Products/Edit.cshtml", model);
        }

        await mediator.Send(new UpdateProductCommand(id, model.Name, model.Slug, model.Description, model.Price, model.CategoryId, model.IsActive, model.IsFeatured), cancellationToken);
        await mediator.Send(new UpdateStockCommand(id, model.Stock), cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
