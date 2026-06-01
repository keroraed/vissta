using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Enums;
using VISSTA.Domain.ValueObjects;
using VISSTA.Infrastructure.Settings;

namespace VISSTA.Infrastructure.Services;

public sealed class SmtpEmailService(IOptions<EmailSettings> options, IOptions<SiteSettings> siteOptions, IWebHostEnvironment env, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;
    private readonly SiteSettings _siteSettings = siteOptions.Value;

    public async Task SendOrderConfirmationAsync(OrderConfirmationEmailDto dto)
    {
        var templatePath = Path.Combine(env.WebRootPath, "email-templates", "order-confirmation.html");
        var html = await File.ReadAllTextAsync(templatePath);

        // Build order line rows
        var linesHtml = string.Concat(dto.Lines.Select(line => BuildOrderLineRow(line, dto.Currency)));

        // Format shipping display
        var shippingDisplay = dto.ShippingCost == 0 ? "Free" : $"{dto.Currency} {dto.ShippingCost:N0}";

        // Format discount display
        var discountRowHtml = "";
        if (dto.DiscountAmount > 0)
        {
            var couponLabel = !string.IsNullOrWhiteSpace(dto.CouponCode) ? $" ({dto.CouponCode})" : "";
            discountRowHtml =
                "<tr>" +
                "<td class=\"em-totals-cell em-force-bg\" style=\"padding:12px 18px;border-bottom:1px solid rgba(212,175,115,0.07);\">" +
                $"<span class=\"em-force-text-muted\" style=\"color:rgba(245,241,234,0.45);font-size:0.72rem;letter-spacing:0.12em;" +
                $"text-transform:uppercase;font-family:'Helvetica',Arial,sans-serif;\">Discount{couponLabel}</span>" +
                "</td>" +
                "<td class=\"em-totals-cell em-force-bg\" align=\"right\" style=\"padding:12px 18px;border-bottom:1px solid rgba(212,175,115,0.07);\">" +
                $"<span class=\"em-force-text-muted\" style=\"color:rgba(245,241,234,0.7);font-size:0.78rem;" +
                $"font-family:'Helvetica',Arial,sans-serif;\">-{dto.Currency} {dto.DiscountAmount:N0}</span>" +
                "</td>" +
                "</tr>";
        }

        html = html
            .Replace("{{CUSTOMER_FIRST_NAME}}", dto.CustomerFirstName)
            .Replace("{{ORDER_NUMBER}}", dto.OrderNumber)
            .Replace("{{ORDER_DATE}}", dto.OrderDate.ToString("dd MMMM yyyy"))
            .Replace("{{PAYMENT_SUMMARY}}", dto.PaymentSummary)
            .Replace("{{ESTIMATED_DELIVERY}}", dto.EstimatedDelivery)
            .Replace("{{ORDER_LINES_HTML}}", linesHtml)
            .Replace("{{SUBTOTAL}}", dto.Subtotal.ToString("N0"))
            .Replace("{{SHIPPING}}", shippingDisplay)
            .Replace("{{DISCOUNT_ROW}}", discountRowHtml)
            .Replace("{{ORDER_TOTAL}}", dto.Total.ToString("N0"))
            .Replace("{{CURRENCY}}", dto.Currency)
            .Replace("{{SHIPPING_ADDRESS}}", dto.ShippingAddress)
            .Replace("{{ORDER_TRACKING_URL}}", dto.OrderTrackingUrl)
            .Replace("{{YEAR}}", DateTime.UtcNow.Year.ToString());

        await SendHtmlAsync(dto.ToEmail, $"VISSTA — Order {dto.OrderNumber} Confirmed", html);
    }

    public Task SendShippingUpdateAsync(string toEmail, int orderId, OrderStatus status, CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, $"VISSTA order #{orderId} update", $"Your order is now {status}.", cancellationToken);

    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, "Reset your VISSTA password", $"Reset your password: {resetLink}", cancellationToken);

    public async Task SendPasswordResetOtpAsync(string toEmail, string otp)
    {
        // Build individual gold-box digit cells
        var digitCells = string.Concat(otp.Select(d =>
            $"<td style=\"padding:0 3px;\">" +
            $"<div class=\"em-digit-box\" style=\"width:36px;height:46px;line-height:46px;" +
            $"text-align:center;font-size:1.25rem;font-weight:700;" +
            $"font-family:'Helvetica',Arial,sans-serif;color:#071426;" +
            $"background:#D4AF73;border-radius:2px;display:block;\">" +
            $"{d}</div></td>"));

        // Load template from wwwroot/email-templates/reset-otp.html
        var templatePath = Path.Combine(env.WebRootPath, "email-templates", "reset-otp.html");
        var html = await File.ReadAllTextAsync(templatePath);

        html = html
            .Replace("{{OTP_DIGIT_CELLS}}", digitCells)
            .Replace("{{EXPIRY_MINUTES}}", "10")
            .Replace("{{YEAR}}", DateTime.UtcNow.Year.ToString());

        await SendHtmlAsync(toEmail, "Your VISSTA Reset Code", html);
    }

    public async Task SendNewsletterWelcomeAsync(string toEmail, string unsubscribeToken)
    {
        var dto = new NewsletterCampaignEmailDto(
            "Welcome to VISSTA - The Edit",
            "Welcome to VISSTA",
            "You are now on the list for new drops, private edits, and quiet releases from VISSTA.",
            Array.Empty<NewsletterCampaignProductEmailDto>(),
            _siteSettings.PublicBaseUrl);

        var html = await RenderNewsletterCampaignAsync(dto, toEmail, unsubscribeToken);

        await SendHtmlAsync(toEmail, dto.Subject, html);
    }

    public async Task<string> RenderNewsletterCampaignAsync(NewsletterCampaignEmailDto dto, string toEmail = "", string unsubscribeToken = "")
    {
        var templatePath = Path.Combine(env.WebRootPath, "email-templates", "newsletter-welcome.html");
        var html = await File.ReadAllTextAsync(templatePath);
        var baseUrl = string.IsNullOrWhiteSpace(dto.PublicBaseUrl) ? _siteSettings.PublicBaseUrl : dto.PublicBaseUrl;
        var unsubscribeUrl = string.IsNullOrWhiteSpace(unsubscribeToken)
            ? "#"
            : $"{baseUrl.TrimEnd('/')}/Newsletter/Unsubscribe?token={Uri.EscapeDataString(unsubscribeToken)}";

        html = Regex.Replace(html, "<title>.*?</title>", "<title>{{EMAIL_TITLE}}</title>", RegexOptions.Singleline);
        html = Regex.Replace(
            html,
            "\\s*<!-- New This Week label -->.*?<!-- Pull quote -->",
            Environment.NewLine + "              {{PRODUCTS_SECTION_HTML}}" + Environment.NewLine + Environment.NewLine + "              <!-- Pull quote -->",
            RegexOptions.Singleline);

        html = html
            .Replace("{{EMAIL_TITLE}}", Html(dto.Subject))
            .Replace("{{ISSUE_LABEL}}", "The Edit")
            .Replace("{{EYEBROW}}", "VISSTA NEWSLETTER")
            .Replace("{{HEADLINE_HTML}}", ToTextHtml(dto.Headline))
            .Replace("{{BODY_HTML}}", ToMultilineHtml(dto.Body))
            .Replace("{{PRODUCTS_SECTION_HTML}}", BuildNewsletterProductsSection(dto.Products, baseUrl))
            .Replace("{{CTA_TITLE}}", "Explore VISSTA")
            .Replace("{{CTA_BODY}}", "Discover the pieces currently shaping the collection.")
            .Replace("{{SHOP_URL}}", $"{baseUrl.TrimEnd('/')}/shop")
            .Replace("{{YEAR}}", DateTime.UtcNow.Year.ToString())
            .Replace("{{EMAIL}}", Html(toEmail))
            .Replace("{{UNSUBSCRIBE_URL}}", Html(unsubscribeUrl));

        return html;
    }

    public async Task SendNewsletterCampaignAsync(string toEmail, string unsubscribeToken, NewsletterCampaignEmailDto dto)
    {
        var html = await RenderNewsletterCampaignAsync(dto, toEmail, unsubscribeToken);
        await SendHtmlAsync(toEmail, dto.Subject, html);
    }

    private static string BuildOrderLineRow(OrderLineDto line, string currency)
    {
        return
            "<tr>" +
            "<td style=\"padding:14px 18px;border-bottom:1px solid rgba(212,175,115,0.07);\">" +
            "<table cellpadding='0' cellspacing='0' role='presentation' width='100%'><tr>" +

            // Product image cell
            "<td width='44' valign='middle' style='padding-right:14px;'>" +
            "<table cellpadding='0' cellspacing='0' role='presentation'><tr>" +
            "<td width='44' height='56' align='center' valign='middle' " +
            "style='width:44px;height:56px;background:rgba(212,175,115,0.08);" +
            "border:1px solid rgba(212,175,115,0.12);border-radius:2px;overflow:hidden;'>" +
            $"<img src='{line.ImageUrl}' width='44' height='56' alt='Product' " +
            "style='display:block;width:44px;height:56px;border:none;object-fit:cover;' />" +
            "</td></tr></table></td>" +

            // Name + variant cell
            "<td valign='middle'>" +
            $"<div style='color:#F5F1EA;font-size:0.8rem;letter-spacing:0.06em;" +
            $"text-transform:uppercase;font-family:Helvetica,Arial,sans-serif;" +
            $"font-weight:500;margin-bottom:4px;'>{line.ProductName}</div>" +
            $"<div style='color:rgba(245,241,234,0.38);font-size:0.68rem;" +
            $"letter-spacing:0.1em;font-family:Helvetica,Arial,sans-serif;'>" +
            $"{line.Variant} — Qty {line.Quantity}</div>" +
            "</td>" +

            // Price cell
            $"<td valign='middle' align='right' " +
            $"style='color:#D4AF73;font-size:0.85rem;" +
            $"font-family:Helvetica,Arial,sans-serif;white-space:nowrap;'>" +
            $"{currency} {(line.UnitPrice * line.Quantity):N0}</td>" +

            "</tr></table></td></tr>";
    }

    private static string BuildNewsletterProductsSection(IReadOnlyCollection<NewsletterCampaignProductEmailDto> products, string baseUrl)
    {
        if (products.Count == 0)
        {
            return string.Empty;
        }

        var rows = new List<string>();
        var productArray = products.ToArray();
        for (var i = 0; i < productArray.Length; i += 2)
        {
            var first = BuildNewsletterProductCell(productArray[i], baseUrl, true);
            var second = i + 1 < productArray.Length
                ? BuildNewsletterProductCell(productArray[i + 1], baseUrl, false)
                : "<td class=\"em-product-td\" width=\"50%\" valign=\"top\" style=\"padding-left:7px;\">&nbsp;</td>";

            rows.Add($"<tr>{first}{second}</tr>");
        }

        return
            "<div class=\"em-force-text-gold\" style=\"color:#D4AF73;font-size:0.58rem;letter-spacing:0.26em;" +
            "text-transform:uppercase;margin-bottom:18px;font-family:'Helvetica',Arial,sans-serif;font-weight:700;\">Featured Edit</div>" +
            "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"margin-bottom:28px;\">" +
            string.Concat(rows) +
            "</table>" +
            BuildSectionRule();
    }

    private static string BuildNewsletterProductCell(NewsletterCampaignProductEmailDto product, string baseUrl, bool left)
    {
        var padding = left ? "padding-right:7px;" : "padding-left:7px;";
        var productUrl = AbsoluteUrl($"/shop/{product.Slug}", baseUrl);
        var imageUrl = AbsoluteUrl(product.ImageUrl, baseUrl);
        var price = product.DiscountValue is > 0 ? product.EffectivePrice : product.Price;

        return
            $"<td class=\"em-product-td\" width=\"50%\" valign=\"top\" style=\"{padding}padding-bottom:14px;\">" +
            "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" class=\"em-product-card\" " +
            "style=\"border:1px solid rgba(212,175,115,0.1);border-radius:2px;overflow:hidden;\">" +
            "<tr><td class=\"em-product-img\" align=\"center\" valign=\"middle\" style=\"height:150px;background:rgba(212,175,115,0.07);" +
            "border-bottom:1px solid rgba(212,175,115,0.08);\">" +
            $"<a href=\"{Html(productUrl)}\" target=\"_blank\" style=\"display:block;text-decoration:none;\">" +
            $"<img src=\"{Html(imageUrl)}\" width=\"100%\" height=\"150\" alt=\"{Html(product.Name)}\" " +
            "style=\"display:block;width:100%;height:150px;border:none;object-fit:cover;\" /></a>" +
            "</td></tr>" +
            "<tr><td style=\"padding:14px 16px;\">" +
            "<div class=\"em-force-text-gold\" style=\"color:rgba(212,175,115,0.5);font-size:0.58rem;letter-spacing:0.2em;" +
            $"text-transform:uppercase;margin-bottom:5px;font-family:'Helvetica',Arial,sans-serif;\">{Html(product.CategoryName)}</div>" +
            "<div class=\"em-force-text-cream\" style=\"color:#F5F1EA;font-size:0.8rem;letter-spacing:0.06em;" +
            $"text-transform:uppercase;margin-bottom:4px;font-family:'Helvetica',Arial,sans-serif;font-weight:500;\">{Html(product.Name)}</div>" +
            "<div class=\"em-force-text-gold\" style=\"color:#D4AF73;font-size:0.75rem;font-family:'Helvetica',Arial,sans-serif;\">" +
            $"{Html(product.Currency)} {price:N0}</div>" +
            "</td></tr></table></td>";
    }

    private static string BuildSectionRule() =>
        "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"margin-bottom:28px;\">" +
        "<tr><td style=\"height:1px;background:rgba(212,175,115,0.1);font-size:0;line-height:0;\">&nbsp;</td>" +
        "<td width=\"30\" align=\"center\" style=\"padding:0 8px;background:#071426;\">" +
        "<span class=\"em-force-text-dim\" style=\"color:rgba(212,175,115,0.35);font-size:0.5rem;\">&#9670;</span>" +
        "</td><td style=\"height:1px;background:rgba(212,175,115,0.1);font-size:0;line-height:0;\">&nbsp;</td></tr></table>";

    private static string AbsoluteUrl(string url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            url = "/assets/product-white-polo.webp";
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return url;
        }

        return $"{baseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
    }

    private static string ToTextHtml(string value) => Html(value).Replace("\n", "<br>");

    private static string ToMultilineHtml(string value) =>
        Html(value).Replace("\r\n", "\n").Replace("\n", "<br>");

    private static string Html(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);

    private async Task SendHtmlAsync(string toEmail, string subject, string html)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = html };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);
        if (!string.IsNullOrWhiteSpace(_settings.UserName))
        {
            await client.AuthenticateAsync(_settings.UserName, _settings.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl, cancellationToken);
        if (!string.IsNullOrWhiteSpace(_settings.UserName))
        {
            await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}


public sealed class MockPaymentService(IOptions<PaymentSettings> options) : IPaymentService
{
    public Task<PaymentResult> ChargeAsync(decimal amount, string currency, string paymentToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(options.Value.AlwaysApprove
            ? new PaymentResult(true, $"mock_{Guid.NewGuid():N}")
            : new PaymentResult(false, string.Empty, "Mock payment declined."));
    }
}

public sealed class LocalFileStorageService(IOptions<StorageSettings> options) : IFileStorageService
{
    public async Task<string> SaveAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var safeName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", options.Value.UploadRoot);
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, safeName);
        await using var output = File.Create(path);
        await fileStream.CopyToAsync(output, cancellationToken);
        return $"/{options.Value.UploadRoot}/{safeName}";
    }
}

public sealed class CurrencyFormatter : ICurrencyFormatter
{
    public string Format(Money money) => $"{money.Amount:N0} {money.Currency}";
}
