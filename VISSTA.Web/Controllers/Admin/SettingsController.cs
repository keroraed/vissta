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

    [HttpGet("payments")]
    public async Task<IActionResult> Payments(CancellationToken cancellationToken)
    {
        var values = await settings.QueryReadOnly()
            .Where(x => PaymentSettingKeys.ManualPaymentPhoneNumbers.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);

        var model = new AdminPaymentSettingsViewModel
        {
            InstaPayPhoneNumber = values.GetValueOrDefault(PaymentSettingKeys.InstaPayPhoneNumber),
            VodafoneCashPhoneNumber = values.GetValueOrDefault(PaymentSettingKeys.VodafoneCashPhoneNumber),
            OrangeCashPhoneNumber = values.GetValueOrDefault(PaymentSettingKeys.OrangeCashPhoneNumber),
            EtisalatCashPhoneNumber = values.GetValueOrDefault(PaymentSettingKeys.EtisalatCashPhoneNumber),
            WePayPhoneNumber = values.GetValueOrDefault(PaymentSettingKeys.WePayPhoneNumber)
        };

        return View("~/Views/Admin/Settings/Payments.cshtml", model);
    }

    [HttpPost("payments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Payments(AdminPaymentSettingsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Settings/Payments.cshtml", model);
        }

        await UpsertSettingAsync(PaymentSettingKeys.InstaPayPhoneNumber, model.InstaPayPhoneNumber, cancellationToken);
        await UpsertSettingAsync(PaymentSettingKeys.VodafoneCashPhoneNumber, model.VodafoneCashPhoneNumber, cancellationToken);
        await UpsertSettingAsync(PaymentSettingKeys.OrangeCashPhoneNumber, model.OrangeCashPhoneNumber, cancellationToken);
        await UpsertSettingAsync(PaymentSettingKeys.EtisalatCashPhoneNumber, model.EtisalatCashPhoneNumber, cancellationToken);
        await UpsertSettingAsync(PaymentSettingKeys.WePayPhoneNumber, model.WePayPhoneNumber, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        TempData["SettingsMessage"] = "Payment details updated.";
        return RedirectToAction(nameof(Payments));
    }

    private async Task UpsertSettingAsync(string key, string? value, CancellationToken cancellationToken)
    {
        var setting = await settings.Query()
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (setting is null)
        {
            await settings.AddAsync(new AppSetting(key, value ?? string.Empty), cancellationToken);
            return;
        }

        setting.UpdateValue(value ?? string.Empty);
        settings.Update(setting);
    }
}
