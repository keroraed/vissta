using FluentValidation;
using MediatR;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;

namespace VISSTA.Application.Features.Sizes;

public sealed record GetSizesQuery : IRequest<IReadOnlyCollection<SizeDto>>;
public sealed record GetSizeByIdQuery(int Id) : IRequest<SizeDto?>;
public sealed record CreateSizeCommand(string Name, int DisplayOrder) : IRequest<int>;
public sealed record UpdateSizeCommand(int Id, string Name, int DisplayOrder) : IRequest<bool>;
public sealed record DeleteSizeCommand(int Id) : IRequest<bool>;

public sealed class CreateSizeCommandValidator : AbstractValidator<CreateSizeCommand>
{
    public CreateSizeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(8);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateSizeCommandValidator : AbstractValidator<UpdateSizeCommand>
{
    public UpdateSizeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(8);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class SizeHandlers(
    IRepository<Size> sizes,
    IUnitOfWork unitOfWork) :
    IRequestHandler<GetSizesQuery, IReadOnlyCollection<SizeDto>>,
    IRequestHandler<GetSizeByIdQuery, SizeDto?>,
    IRequestHandler<CreateSizeCommand, int>,
    IRequestHandler<UpdateSizeCommand, bool>,
    IRequestHandler<DeleteSizeCommand, bool>
{
    public Task<IReadOnlyCollection<SizeDto>> Handle(GetSizesQuery request, CancellationToken cancellationToken)
    {
        var items = sizes.QueryReadOnly()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToList();

        var dtos = items.Select(x => new SizeDto(x.Id, x.Name, x.DisplayOrder)).ToList();
        return Task.FromResult<IReadOnlyCollection<SizeDto>>(dtos);
    }

    public async Task<SizeDto?> Handle(GetSizeByIdQuery request, CancellationToken cancellationToken)
    {
        var size = await sizes.GetByIdAsync(request.Id, cancellationToken);
        return size is null ? null : new SizeDto(size.Id, size.Name, size.DisplayOrder);
    }

    public async Task<int> Handle(CreateSizeCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim().ToUpperInvariant();
        var exists = sizes.QueryReadOnly().Any(x => x.Name == normalizedName);
        if (exists)
        {
            throw new InvalidOperationException($"Size '{request.Name}' already exists.");
        }

        var size = new Size(normalizedName, request.DisplayOrder);
        await sizes.AddAsync(size, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return size.Id;
    }

    public async Task<bool> Handle(UpdateSizeCommand request, CancellationToken cancellationToken)
    {
        var size = await sizes.GetByIdAsync(request.Id, cancellationToken);
        if (size is null)
        {
            return false;
        }

        var normalizedName = request.Name.Trim().ToUpperInvariant();
        var exists = sizes.QueryReadOnly()
            .Any(x => x.Name == normalizedName && x.Id != request.Id);
        if (exists)
        {
            throw new InvalidOperationException($"Size '{request.Name}' already exists.");
        }

        size.Update(normalizedName, request.DisplayOrder);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteSizeCommand request, CancellationToken cancellationToken)
    {
        var size = await sizes.GetByIdAsync(request.Id, cancellationToken);
        if (size is null)
        {
            return false;
        }

        sizes.Remove(size);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
