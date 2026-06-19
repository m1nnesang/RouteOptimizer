using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;

namespace RouteOptimizer.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db)
    {
        _db = db;
    }


    public async Task AddAsync(Order order, CancellationToken ct)
    {
        await _db.Orders.AddAsync(order, ct);
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Orders
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<Order>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        return await _db.Orders
            .Where(o => ids.Contains(o.Id))
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAllAsync(Guid? warehouseId, OrderStatus? status, DateOnly? date, int skip, int take, CancellationToken ct)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (warehouseId is not null)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (status is not null)
            query = query.Where(x => x.Status == status.Value);

        if (date is not null)
        {
            var from = DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var to = DateTime.SpecifyKind(date.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt >= from && x.CreatedAt < to);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);

        return (items, totalCount);
    }
}
