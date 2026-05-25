using FluentValidation;
using MediatR;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;

namespace VISSTA.Application.Features.Reviews;

public sealed record SubmitReviewCommand(int ProductId, string CustomerId, string CustomerName, string CustomerPhone, int Rating, string Body) : IRequest<int>;
public sealed record ApproveReviewCommand(int ReviewId) : IRequest<bool>;
public sealed record GetProductReviewsQuery(int ProductId) : IRequest<IReadOnlyCollection<ReviewDto>>;
public sealed record GetRecentReviewsQuery(int Count = 8, bool IncludePending = true) : IRequest<IReadOnlyCollection<ReviewDto>>;
public sealed record GetReviewListQuery(bool IncludePending = true) : IRequest<IReadOnlyCollection<ReviewDto>>;

public sealed class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(1500);
    }
}

public sealed class ReviewHandlers(IRepository<Review> reviews, IRepository<Customer> customers, IUnitOfWork unitOfWork) :
    IRequestHandler<SubmitReviewCommand, int>,
    IRequestHandler<ApproveReviewCommand, bool>,
    IRequestHandler<GetProductReviewsQuery, IReadOnlyCollection<ReviewDto>>,
    IRequestHandler<GetRecentReviewsQuery, IReadOnlyCollection<ReviewDto>>,
    IRequestHandler<GetReviewListQuery, IReadOnlyCollection<ReviewDto>>
{
    public async Task<int> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
    {
        var customer = customers.Query().FirstOrDefault(x => x.Id == request.CustomerId);
        if (customer is null)
        {
            await customers.AddAsync(new Customer(request.CustomerId, request.CustomerName, request.CustomerPhone), cancellationToken);
        }

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
            .Select(x => new ReviewDto(
                x.Id,
                x.Customer == null ? "VISSTA Customer" : x.Customer.FullName,
                x.Rating,
                x.Body,
                x.CreatedAt,
                x.ProductId,
                x.Product == null ? "VISSTA Product" : x.Product.Name,
                x.Product == null ? string.Empty : x.Product.Slug,
                x.IsApproved))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<ReviewDto>>(items);
    }

    public Task<IReadOnlyCollection<ReviewDto>> Handle(GetRecentReviewsQuery request, CancellationToken cancellationToken)
    {
        var query = reviews.QueryReadOnly();
        if (!request.IncludePending)
        {
            query = query.Where(x => x.IsApproved);
        }

        var items = query
            .OrderBy(x => x.IsApproved)
            .ThenByDescending(x => x.CreatedAt)
            .Take(request.Count)
            .Select(x => new ReviewDto(
                x.Id,
                x.Customer == null ? "VISSTA Customer" : x.Customer.FullName,
                x.Rating,
                x.Body,
                x.CreatedAt,
                x.ProductId,
                x.Product == null ? "VISSTA Product" : x.Product.Name,
                x.Product == null ? string.Empty : x.Product.Slug,
                x.IsApproved))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<ReviewDto>>(items);
    }

    public Task<IReadOnlyCollection<ReviewDto>> Handle(GetReviewListQuery request, CancellationToken cancellationToken)
    {
        var query = reviews.QueryReadOnly();
        if (!request.IncludePending)
        {
            query = query.Where(x => x.IsApproved);
        }

        var items = query
            .OrderBy(x => x.IsApproved)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new ReviewDto(
                x.Id,
                x.Customer == null ? "VISSTA Customer" : x.Customer.FullName,
                x.Rating,
                x.Body,
                x.CreatedAt,
                x.ProductId,
                x.Product == null ? "VISSTA Product" : x.Product.Name,
                x.Product == null ? string.Empty : x.Product.Slug,
                x.IsApproved))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<ReviewDto>>(items);
    }
}
