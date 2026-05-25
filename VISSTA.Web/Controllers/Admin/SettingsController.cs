using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/settings")]
public sealed class SettingsController(IRepository<AppSetting> settings, IUnitOfWork unitOfWork) : Controller
{
    private const string LowStockKey = "LowStockThreshold";
    private const int DefaultThreshold = 5;

    [HttpGet("stock")]
    public async Task<IActionResult> Stock(CancellationToken cancellationToken)
    {
        var value = await settings.QueryReadOnly()
            .Where(x => x.Key == LowStockKey)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);

        var model = new AdminStockSettingsViewModel
        {
            LowStockThreshold = int.TryParse(value, out var threshold) ? threshold : DefaultThreshold
        };

        return View("~/Views/Admin/Settings/Stock.cshtml", model);
    }

    [HttpPost("stock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Stock(AdminStockSettingsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Settings/Stock.cshtml", model);
        }

        var setting = await settings.Query()
            .FirstOrDefaultAsync(x => x.Key == LowStockKey, cancellationToken);

        if (setting is null)
        {
            await settings.AddAsync(new AppSetting(LowStockKey, model.LowStockThreshold.ToString()), cancellationToken);
        }
        else
        {
            setting.UpdateValue(model.LowStockThreshold.ToString());
            settings.Update(setting);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        TempData["SettingsMessage"] = "Stock threshold updated.";
        return RedirectToAction(nameof(Stock));
    }
}
