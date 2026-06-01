using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;

namespace VISSTA.Application.Features.Newsletter;

// ── Subscribe ───────────────────────────────────────────────

public sealed record SubscribeNewsletterCommand(string Email) : IRequest<SubscribeNewsletterResult>;

public sealed record SubscribeNewsletterResult(bool Success, bool AlreadySubscribed, string Message);

public sealed class SubscribeNewsletterCommandValidator : AbstractValidator<SubscribeNewsletterCommand>
{
    public SubscribeNewsletterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class SubscribeNewsletterHandler(
    INewsletterRepository newsletterRepository,
    IEmailService emailService,
    IAdminNotificationService adminNotifications,
    ILogger<SubscribeNewsletterHandler> logger) : IRequestHandler<SubscribeNewsletterCommand, SubscribeNewsletterResult>
{
    public async Task<SubscribeNewsletterResult> Handle(SubscribeNewsletterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.ToLowerInvariant().Trim();

        var existing = await newsletterRepository.GetByEmailAsync(normalizedEmail);

        if (existing is not null && existing.IsActive)
        {
            return new SubscribeNewsletterResult(true, true, "You're already on our list.");
        }

        if (existing is not null && !existing.IsActive)
        {
            existing.Resubscribe();
            await newsletterRepository.SaveChangesAsync();

            try
            {
                await emailService.SendNewsletterWelcomeAsync(normalizedEmail, existing.UnsubscribeToken ?? "");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send newsletter welcome email to {Email}", normalizedEmail);
            }

            await adminNotifications.NotifyNewSubscriberAsync(normalizedEmail, cancellationToken);
            return new SubscribeNewsletterResult(true, false, "Welcome back! You've been re-subscribed.");
        }

        var subscriber = NewsletterSubscriber.Create(normalizedEmail);
        await newsletterRepository.AddAsync(subscriber);
        await newsletterRepository.SaveChangesAsync();

        try
        {
            await emailService.SendNewsletterWelcomeAsync(normalizedEmail, subscriber.UnsubscribeToken ?? "");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send newsletter welcome email to {Email}", normalizedEmail);
        }

        await adminNotifications.NotifyNewSubscriberAsync(normalizedEmail, cancellationToken);
        return new SubscribeNewsletterResult(true, false, "You're in! Welcome to VISSTA.");
    }
}

// ── Unsubscribe ─────────────────────────────────────────────

public sealed record UnsubscribeNewsletterCommand(string Token) : IRequest<UnsubscribeResult>;

public sealed record UnsubscribeResult(bool Success, string Message);

public sealed class UnsubscribeNewsletterHandler(
    INewsletterRepository newsletterRepository) : IRequestHandler<UnsubscribeNewsletterCommand, UnsubscribeResult>
{
    public async Task<UnsubscribeResult> Handle(UnsubscribeNewsletterCommand request, CancellationToken cancellationToken)
    {
        var subscriber = await newsletterRepository.GetByTokenAsync(request.Token);

        if (subscriber is null)
        {
            return new UnsubscribeResult(false, "Token not found.");
        }

        subscriber.Unsubscribe();
        await newsletterRepository.SaveChangesAsync();

        return new UnsubscribeResult(true, "You've been unsubscribed.");
    }
}
