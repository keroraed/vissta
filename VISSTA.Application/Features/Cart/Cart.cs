using FluentValidation;
using MediatR;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;

namespace VISSTA.Application.Features.Cart;

public sealed record GetCartQuery(string? CustomerId, string SessionId) : IRequest<CartDto>;
public sealed record AddToCartCommand(string? CustomerId, string SessionId, int ProductId, string Size, int Quantity) : IRequest<CartDto>;
public sealed record RemoveFromCartCommand(string? CustomerId, string SessionId, int CartItemId) : IRequest<CartDto>;
public sealed record UpdateCartItemCommand(string? CustomerId, string SessionId, int CartItemId, int Quantity) : IRequest<CartDto>;
public sealed record ClearCartCommand(string? CustomerId, string SessionId) : IRequest<CartDto>;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Size).NotEmpty().Must(size => size.Trim().ToUpperInvariant() is "S" or "M" or "L" or "XL");
        RuleFor(x => x.Quantity).InclusiveBetween(1, 20);
    }
}

public sealed class CartHandlers(ICartRepository carts, IProductRepository products, IRepository<Customer> customers, IUnitOfWork unitOfWork) :
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
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
        {
            throw new InvalidOperationException("Product is not available.");
        }

        var requestedSize = Product.NormalizeSize(request.Size);
        var existingQuantity = cart.CartItems
            .Where(x => x.ProductId == request.ProductId && x.Size == requestedSize)
            .Sum(x => x.Quantity);
        if (existingQuantity + request.Quantity > product.GetStockForSize(requestedSize))
        {
            throw new InvalidOperationException("Selected size does not have enough stock.");
        }

        cart.AddItem(request.ProductId, request.Size, request.Quantity);
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
        var resolvedCustomerId = customerId;
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            var customerExists = customers.QueryReadOnly().Any(x => x.Id == customerId);
            if (!customerExists)
            {
                resolvedCustomerId = null;
            }
        }

        var cart = await carts.GetActiveCartAsync(resolvedCustomerId, sessionId, cancellationToken);
        if (cart is not null)
        {
            return cart;
        }

        cart = new Domain.Entities.Cart(resolvedCustomerId, sessionId);
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
                price * x.Quantity,
                x.Size);
        }).ToList();

        return new CartDto(cart.Id, items, items.Sum(x => x.LineTotal), items.FirstOrDefault()?.Currency ?? "EGP", items.Sum(x => x.Quantity));
    }
}
