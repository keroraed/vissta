using FluentValidation;
using MediatR;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;

namespace VISSTA.Application.Features.Cart;

public sealed record GetCartQuery(string? CustomerId, string SessionId) : IRequest<CartDto>;
public sealed record AddToCartCommand(string? CustomerId, string SessionId, int ProductId, int Quantity) : IRequest<CartDto>;
public sealed record RemoveFromCartCommand(string? CustomerId, string SessionId, int CartItemId) : IRequest<CartDto>;
public sealed record UpdateCartItemCommand(string? CustomerId, string SessionId, int CartItemId, int Quantity) : IRequest<CartDto>;
public sealed record ClearCartCommand(string? CustomerId, string SessionId) : IRequest<CartDto>;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 20);
    }
}

public sealed class CartHandlers(ICartRepository carts, IUnitOfWork unitOfWork) :
    IRequestHandler<GetCartQuery, CartDto>,
    IRequestHandler<AddToCartCommand, CartDto>,
    IRequestHandler<RemoveFromCartCommand, CartDto>,
    IRequestHandler<UpdateCartItemCommand, CartDto>,
    IRequestHandler<ClearCartCommand, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await GetOrCreateCart(request.CustomerId, request.SessionId, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await GetOrCreateCart(request.CustomerId, request.SessionId, cancellationToken);
        cart.AddItem(request.ProductId, request.Quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await GetOrCreateCart(request.CustomerId, request.SessionId, cancellationToken);
        cart.RemoveItem(request.CartItemId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await GetOrCreateCart(request.CustomerId, request.SessionId, cancellationToken);
        cart.CartItems.FirstOrDefault(x => x.Id == request.CartItemId)?.UpdateQuantity(request.Quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await GetOrCreateCart(request.CustomerId, request.SessionId, cancellationToken);
        cart.Clear();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(cart);
    }

    private async Task<Domain.Entities.Cart> GetOrCreateCart(string? customerId, string sessionId, CancellationToken cancellationToken)
    {
        var cart = await carts.GetActiveCartAsync(customerId, sessionId, cancellationToken);
        if (cart is not null)
        {
            return cart;
        }

        cart = new Domain.Entities.Cart(customerId, sessionId);
        await carts.AddAsync(cart, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private static CartDto ToDto(Domain.Entities.Cart cart)
    {
        var items = cart.CartItems.Select(x =>
        {
            var product = x.Product;
            var price = product?.Price.Amount ?? 0;
            var currency = product?.Price.Currency ?? "EGP";
            return new CartItemDto(
                x.Id,
                x.ProductId,
                product?.Name ?? "VISSTA Product",
                product?.Slug ?? "#",
                product?.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? "/assets/product-white-polo.webp",
                price,
                currency,
                x.Quantity,
                price * x.Quantity);
        }).ToList();

        return new CartDto(cart.Id, items, items.Sum(x => x.LineTotal), items.FirstOrDefault()?.Currency ?? "EGP", items.Sum(x => x.Quantity));
    }
}
