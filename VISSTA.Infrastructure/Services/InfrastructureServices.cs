using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Enums;
using VISSTA.Domain.ValueObjects;
using VISSTA.Infrastructure.Settings;

namespace VISSTA.Infrastructure.Services;

public sealed class SmtpEmailService(IOptions<EmailSettings> options, IWebHostEnvironment env) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public Task SendOrderConfirmationAsync(string toEmail, int orderId, CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, $"VISSTA order #{orderId} confirmed", $"Your order #{orderId} has been confirmed.", cancellationToken);

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

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Your VISSTA Reset Code";

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
