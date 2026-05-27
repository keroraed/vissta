using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Enums;
using VISSTA.Domain.ValueObjects;
using VISSTA.Infrastructure.Settings;

namespace VISSTA.Infrastructure.Services;

public sealed class SmtpEmailService(IOptions<EmailSettings> options, IWebHostEnvironment env, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendOrderConfirmationAsync(OrderConfirmationEmailDto dto)
    {
        var templatePath = Path.Combine(env.WebRootPath, "email-templates", "order-confirmation.html");
        var html = await File.ReadAllTextAsync(templatePath);

        // Build order line rows
        var linesHtml = string.Concat(dto.Lines.Select(line => BuildOrderLineRow(line, dto.Currency)));

        // Format shipping display
        var shippingDisplay = dto.ShippingCost == 0 ? "Free" : $"{dto.Currency} {dto.ShippingCost:N0}";

        html = html
            .Replace("{{CUSTOMER_FIRST_NAME}}", dto.CustomerFirstName)
            .Replace("{{ORDER_NUMBER}}", dto.OrderNumber)
            .Replace("{{ORDER_DATE}}", dto.OrderDate.ToString("dd MMMM yyyy"))
            .Replace("{{PAYMENT_SUMMARY}}", dto.PaymentSummary)
            .Replace("{{ESTIMATED_DELIVERY}}", dto.EstimatedDelivery)
            .Replace("{{ORDER_LINES_HTML}}", linesHtml)
            .Replace("{{SUBTOTAL}}", dto.Subtotal.ToString("N0"))
            .Replace("{{SHIPPING}}", shippingDisplay)
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
        var templatePath = Path.Combine(env.WebRootPath, "email-templates", "newsletter-welcome.html");
        var html = await File.ReadAllTextAsync(templatePath);

        html = html
            .Replace("{{YEAR}}", DateTime.UtcNow.Year.ToString())
            .Replace("{{EMAIL}}", toEmail)
            .Replace("{{UNSUBSCRIBE_TOKEN}}", unsubscribeToken ?? "");

        await SendHtmlAsync(toEmail, "Welcome to VISSTA — The Edit", html);
    }

    private static string BuildOrderLineRow(OrderLineDto line, string currency)
    {
        return
            "<tr>" +
            "<td style=\"padding:14px 18px;border-bottom:1px solid rgba(212,175,115,0.07);\">" +
            "<table cellpadding='0' cellspacing='0' role='presentation' width='100%'><tr>" +

            // Product icon cell
            "<td width='44' valign='middle' style='padding-right:14px;'>" +
            "<table cellpadding='0' cellspacing='0' role='presentation'><tr>" +
            "<td width='44' height='56' align='center' valign='middle' " +
            "style='width:44px;height:56px;background:rgba(212,175,115,0.08);" +
            "border:1px solid rgba(212,175,115,0.12);border-radius:2px;'>" +
            "<svg width='18' height='18' viewBox='0 0 24 24' fill='none' " +
            "stroke='rgba(212,175,115,0.4)' stroke-width='1.6' " +
            "stroke-linecap='round' stroke-linejoin='round' " +
            "style='display:inline-block;vertical-align:middle;'>" +
            "<path d='M20.38 18H3.62a1 1 0 0 1-.74-1.67L12 7' stroke='rgba(212,175,115,0.4)'/>" +
            "<path d='M12 7V3' stroke='rgba(212,175,115,0.4)'/>" +
            "<circle cx='12' cy='2.5' r='0.5' fill='rgba(212,175,115,0.4)' stroke='none'/>" +
            "</svg>" +
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
