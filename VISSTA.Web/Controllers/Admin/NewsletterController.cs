using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Domain.Enums;
using VISSTA.Infrastructure.Persistence;
using VISSTA.Infrastructure.Settings;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/newsletter")]
public sealed class NewsletterController(
    VISSTADbContext db,
    IEmailService emailService,
    IOptions<SiteSettings> siteOptions,
    ILogger<NewsletterController> logger) : Controller
{
    private readonly SiteSettings _siteSettings = siteOptions.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var campaigns = await db.NewsletterCampaigns
            .Include(x => x.Products)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new AdminNewsletterCampaignListItemViewModel(
                x.Id,
                x.Subject,
                x.Headline,
                x.Status,
                x.UpdatedAt,
                x.ScheduledAt,
                x.SentAt,
                x.SentCount,
                x.Products.Count))
            .ToListAsync(cancellationToken);

        return View("~/Views/Admin/Newsletter/Index.cshtml", new AdminNewsletterCampaignsViewModel(campaigns));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new AdminNewsletterCampaignFormViewModel
        {
            Subject = "VISSTA - The Edit",
            Headline = "New from VISSTA",
            Body = "A considered selection from the latest edit.",
            Products = await LoadProductOptionsAsync([], cancellationToken)
        };

        return View("~/Views/Admin/Newsletter/Create.cshtml", model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminNewsletterCampaignFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateSelectedProducts(model);
        if (!ModelState.IsValid)
        {
            model.Products = await LoadProductOptionsAsync(model.SelectedProductIds, cancellationToken);
            return View("~/Views/Admin/Newsletter/Create.cshtml", model);
        }

        var campaign = new NewsletterCampaign(model.Subject, model.Headline, model.Body);
        campaign.ReplaceProducts(model.SelectedProductIds);
        db.NewsletterCampaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken);

        TempData["NewsletterAdminMessage"] = "Campaign draft created.";
        return RedirectToAction(nameof(Edit), new { id = campaign.Id });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var campaign = await LoadCampaignAsync(id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Newsletter/Edit.cshtml", await ToFormModelAsync(campaign, cancellationToken));
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminNewsletterCampaignFormViewModel model, CancellationToken cancellationToken)
    {
        var campaign = await LoadCampaignAsync(id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.Status == NewsletterCampaignStatus.Sent)
        {
            TempData["NewsletterAdminMessage"] = "Sent campaigns are locked.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        ValidateSelectedProducts(model);
        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.Status = campaign.Status;
            model.Products = await LoadProductOptionsAsync(model.SelectedProductIds, cancellationToken);
            return View("~/Views/Admin/Newsletter/Edit.cshtml", model);
        }

        campaign.UpdateContent(model.Subject, model.Headline, model.Body);
        campaign.ReplaceProducts(model.SelectedProductIds);
        await db.SaveChangesAsync(cancellationToken);

        TempData["NewsletterAdminMessage"] = "Campaign saved.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet("preview/{id:int}")]
    public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
    {
        var campaign = await LoadCampaignAsync(id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        var html = await emailService.RenderNewsletterCampaignAsync(BuildEmailDto(campaign), "preview@vissta.com", string.Empty);
        return Content(html, "text/html");
    }

    [HttpPost("send-test/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTest(int id, string testEmail, CancellationToken cancellationToken)
    {
        var campaign = await LoadCampaignAsync(id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(testEmail))
        {
            TempData["NewsletterAdminMessage"] = "Enter an email address for the test send.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        await emailService.SendNewsletterCampaignAsync(testEmail.Trim(), string.Empty, BuildEmailDto(campaign));
        TempData["NewsletterAdminMessage"] = $"Test email sent to {testEmail.Trim()}.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("send/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNow(int id, CancellationToken cancellationToken)
    {
        var campaign = await LoadCampaignAsync(id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.Status == NewsletterCampaignStatus.Sent)
        {
            TempData["NewsletterAdminMessage"] = "This campaign has already been sent.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var sentCount = await SendToSubscribersAsync(campaign, cancellationToken);
        if (sentCount == 0)
        {
            TempData["NewsletterAdminMessage"] = "No active subscribers found.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        campaign.MarkSent(sentCount);
        await db.SaveChangesAsync(cancellationToken);

        TempData["NewsletterAdminMessage"] = $"Campaign sent to {sentCount} subscriber{(sentCount == 1 ? "" : "s")}.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("schedule/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Schedule(int id, DateTime scheduledAt, CancellationToken cancellationToken)
    {
        var campaign = await LoadCampaignAsync(id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.Status == NewsletterCampaignStatus.Sent)
        {
            TempData["NewsletterAdminMessage"] = "Sent campaigns cannot be scheduled.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var scheduledAtUtc = DateTime.SpecifyKind(scheduledAt, DateTimeKind.Local).ToUniversalTime();
        if (scheduledAtUtc <= DateTime.UtcNow)
        {
            TempData["NewsletterAdminMessage"] = "Choose a future date and time.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        campaign.Schedule(scheduledAtUtc);
        await db.SaveChangesAsync(cancellationToken);

        TempData["NewsletterAdminMessage"] = "Campaign scheduled.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("draft/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDraft(int id, CancellationToken cancellationToken)
    {
        var campaign = await LoadCampaignAsync(id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        campaign.MarkDraft();
        await db.SaveChangesAsync(cancellationToken);

        TempData["NewsletterAdminMessage"] = "Campaign returned to draft.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task<NewsletterCampaign?> LoadCampaignAsync(int id, CancellationToken cancellationToken) =>
        await db.NewsletterCampaigns
            .Include(x => x.Products).ThenInclude(x => x.Product).ThenInclude(x => x!.Images)
            .Include(x => x.Products).ThenInclude(x => x.Product).ThenInclude(x => x!.Category)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private async Task<AdminNewsletterCampaignFormViewModel> ToFormModelAsync(NewsletterCampaign campaign, CancellationToken cancellationToken)
    {
        var selectedIds = campaign.Products
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.ProductId)
            .ToArray();

        return new AdminNewsletterCampaignFormViewModel
        {
            Id = campaign.Id,
            Subject = campaign.Subject,
            Headline = campaign.Headline,
            Body = campaign.Body,
            Status = campaign.Status,
            ScheduledAt = campaign.ScheduledAt?.ToLocalTime(),
            SentAt = campaign.SentAt?.ToLocalTime(),
            SentCount = campaign.SentCount,
            SelectedProductIds = selectedIds,
            Products = await LoadProductOptionsAsync(selectedIds, cancellationToken)
        };
    }

    private async Task<IReadOnlyCollection<AdminNewsletterProductOptionViewModel>> LoadProductOptionsAsync(IReadOnlyCollection<int> selectedIds, CancellationToken cancellationToken)
    {
        var selectedSet = selectedIds.ToHashSet();
        return await db.Products
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Where(x => x.IsActive)
            .OrderByDescending(x => selectedSet.Contains(x.Id))
            .ThenByDescending(x => x.Id)
            .Select(x => new AdminNewsletterProductOptionViewModel(
                x.Id,
                x.Name,
                x.Category == null ? "VISSTA" : x.Category.Name,
                x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? "/assets/product-white-polo.webp",
                x.EffectivePrice,
                x.Price.Currency,
                selectedSet.Contains(x.Id)))
            .ToListAsync(cancellationToken);
    }

    private NewsletterCampaignEmailDto BuildEmailDto(NewsletterCampaign campaign)
    {
        var products = campaign.Products
            .OrderBy(x => x.DisplayOrder)
            .Where(x => x.Product is not null && x.Product.IsActive)
            .Select(x => new NewsletterCampaignProductEmailDto(
                x.Product!.Name,
                x.Product.Slug,
                x.Product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? "/assets/product-white-polo.webp",
                x.Product.Category == null ? "VISSTA" : x.Product.Category.Name,
                x.Product.Price.Amount,
                x.Product.EffectivePrice,
                x.Product.Price.Currency,
                x.Product.DiscountType,
                x.Product.DiscountValue))
            .ToList();

        return new NewsletterCampaignEmailDto(campaign.Subject, campaign.Headline, campaign.Body, products, _siteSettings.PublicBaseUrl);
    }

    private async Task<int> SendToSubscribersAsync(NewsletterCampaign campaign, CancellationToken cancellationToken)
    {
        var subscribers = await db.NewsletterSubscribers
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var dto = BuildEmailDto(campaign);
        var sentCount = 0;
        foreach (var subscriber in subscribers)
        {
            try
            {
                await emailService.SendNewsletterCampaignAsync(subscriber.Email, subscriber.UnsubscribeToken ?? string.Empty, dto);
                sentCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send newsletter campaign {CampaignId} to {Email}", campaign.Id, subscriber.Email);
            }
        }

        return sentCount;
    }

    private void ValidateSelectedProducts(AdminNewsletterCampaignFormViewModel model)
    {
        if (model.SelectedProductIds.Length == 0)
        {
            ModelState.AddModelError(nameof(model.SelectedProductIds), "Select at least one product.");
        }
    }
}
