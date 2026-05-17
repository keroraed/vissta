using FluentValidation;
using MediatR;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;

namespace VISSTA.Application.Features.Reviews;

public sealed record SubmitReviewCommand(int ProductId, string CustomerId, int Rating, string Body) : IRequest<int>;
public sealed record ApproveReviewCommand(int ReviewId) : IRequest<bool>;
public sealed record GetProductReviewsQuery(int ProductId) : IRequest<IReadOnlyCollection<ReviewDto>>;

public sealed class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(1500);
    }
}

public sealed class ReviewHandlers(IRepository<Review> reviews, IUnitOfWork unitOfWork) :
    IRequestHandler<SubmitReviewCommand, int>,
    IRequestHandler<ApproveReviewCommand, bool>,
    IRequestHandler<GetProductReviewsQuery, IReadOnlyCollection<ReviewDto>>
{
    public async Task<int> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
    {
        var review = new Review(request.ProductId, request.CustomerId, request.Rating, request.Body);
        await reviews.AddAsync(review, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return review.Id;
    }

    public async Task<bool> Handle(ApproveReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviews.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
        {
            return false;
        }

        review.Approve();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<IReadOnlyCollection<ReviewDto>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var items = reviews.QueryReadOnly()
            .Where(x => x.ProductId == request.ProductId && x.IsApproved)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReviewDto(x.Id, x.Customer == null ? "VISSTA Customer" : x.Customer.FullName, x.Rating, x.Body, x.CreatedAt))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<ReviewDto>>(items);
    }
}
