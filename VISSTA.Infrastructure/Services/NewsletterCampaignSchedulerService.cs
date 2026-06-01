using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Domain.Enums;
using VISSTA.Infrastructure.Persistence;
using VISSTA.Infrastructure.Settings;

namespace VISSTA.Infrastructure.Services;

public sealed class NewsletterCampaignSchedulerService(
    IServiceScopeFactory scopeFactory,
    ILogger<NewsletterCampaignSchedulerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueCampaignsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Newsletter campaign scheduler failed.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task ProcessDueCampaignsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VISSTADbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var siteSettings = scope.ServiceProvider.GetRequiredService<IOptions<SiteSettings>>().Value;

        var campaigns = await db.NewsletterCampaigns
            .Include(x => x.Products).ThenInclude(x => x.Product).ThenInclude(x => x!.Images)
            .Include(x => x.Products).ThenInclude(x => x.Product).ThenInclude(x => x!.Category)
            .Where(x => x.Status == NewsletterCampaignStatus.Scheduled
                && x.ScheduledAt != null
                && x.ScheduledAt <= DateTime.UtcNow)
            .OrderBy(x => x.ScheduledAt)
            .ToListAsync(cancellationToken);

        foreach (var campaign in campaigns)
        {
            var subscribers = await db.NewsletterSubscribers
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var dto = BuildEmailDto(campaign, siteSettings.PublicBaseUrl);
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
                    logger.LogError(ex, "Failed to send scheduled newsletter campaign {CampaignId} to {Email}", campaign.Id, subscriber.Email);
                }
            }

            campaign.MarkSent(sentCount);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Scheduled newsletter campaign {CampaignId} sent to {SentCount} subscribers.", campaign.Id, sentCount);
        }
    }

    private static NewsletterCampaignEmailDto BuildEmailDto(NewsletterCampaign campaign, string publicBaseUrl)
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

        return new NewsletterCampaignEmailDto(campaign.Subject, campaign.Headline, campaign.Body, products, publicBaseUrl);
    }
}
