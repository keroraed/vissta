using FluentValidation;
using MediatR;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;

namespace VISSTA.Application.Features.Newsletter;

public sealed record SubscribeNewsletterCommand(string Email) : IRequest<bool>;

public sealed class SubscribeNewsletterCommandValidator : AbstractValidator<SubscribeNewsletterCommand>
{
    public SubscribeNewsletterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
    }
}

public sealed class NewsletterHandler(IRepository<NewsletterSubscription> subscriptions, IUnitOfWork unitOfWork) : IRequestHandler<SubscribeNewsletterCommand, bool>
{
    public async Task<bool> Handle(SubscribeNewsletterCommand request, CancellationToken cancellationToken)
    {
        if (subscriptions.QueryReadOnly().Any(x => x.Email == request.Email))
        {
            return true;
        }

        await subscriptions.AddAsync(new NewsletterSubscription(request.Email), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
