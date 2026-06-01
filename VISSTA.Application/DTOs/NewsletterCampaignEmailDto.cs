namespace VISSTA.Application.DTOs;

public sealed record NewsletterCampaignEmailDto(
    string Subject,
    string Headline,
    string Body,
    IReadOnlyCollection<NewsletterCampaignProductEmailDto> Products,
    string PublicBaseUrl);

public sealed record NewsletterCampaignProductEmailDto(
    string Name,
    string Slug,
    string ImageUrl,
    string CategoryName,
    decimal Price,
    decimal EffectivePrice,
    string Currency,
    string? DiscountType,
    decimal? DiscountValue);
