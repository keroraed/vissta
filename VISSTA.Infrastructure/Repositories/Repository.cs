using Microsoft.EntityFrameworkCore;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Infrastructure.Persistence;

namespace VISSTA.Infrastructure.Repositories;

public class Repository<T>(VISSTADbContext dbContext) : IRepository<T> where T : class
{
    protected VISSTADbContext DbContext { get; } = dbContext;

    public virtual IQueryable<T> Query() => DbContext.Set<T>();

    public virtual IQueryable<T> QueryReadOnly() => DbContext.Set<T>().AsNoTracking();

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await DbContext.Set<T>().FindAsync([id], cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbContext.Set<T>().AddAsync(entity, cancellationToken);

    public virtual void Update(T entity) => DbContext.Set<T>().Update(entity);

    public virtual void Remove(T entity) => DbContext.Set<T>().Remove(entity);
}

public sealed class ProductRepository(VISSTADbContext dbContext) : Repository<Product>(dbContext), IProductRepository
{
    public override IQueryable<Product> Query() => DbContext.Products
        .Include(x => x.Category)
        .Include(x => x.Images)
        .Include(x => x.Reviews).ThenInclude(x => x.Customer)
        .Include(x => x.SizeStocks).ThenInclude(x => x.Size);

    public override IQueryable<Product> QueryReadOnly() => Query().AsNoTracking();

    public override async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await Query().FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, cancellationToken);
}

public sealed class OrderRepository(VISSTADbContext dbContext) : Repository<Order>(dbContext), IOrderRepository
{
    public override IQueryable<Order> Query() => DbContext.Orders
        .Include(x => x.Customer)
        .Include(x => x.OrderItems)
        .ThenInclude(x => x.Product)
        .ThenInclude(x => x!.Images);

    public override IQueryable<Order> QueryReadOnly() => Query().AsNoTracking();

    public override async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}

public sealed class CartRepository(VISSTADbContext dbContext) : Repository<Cart>(dbContext), ICartRepository
{
    public override IQueryable<Cart> Query() => DbContext.Carts
        .Include(x => x.CartItems)
        .ThenInclude(x => x.Product)
        .ThenInclude(x => x!.Images);

    public override IQueryable<Cart> QueryReadOnly() => Query().AsNoTracking();

    public override async Task<Cart?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Cart?> GetActiveCartAsync(string? customerId, string sessionId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => (customerId != null && x.CustomerId == customerId) || x.SessionId == sessionId, cancellationToken);
    }
}

public sealed class UnitOfWork(VISSTADbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
