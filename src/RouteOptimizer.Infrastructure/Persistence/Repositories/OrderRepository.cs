using Microsoft.EntityFrameworkCore;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Entities.Orders;

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
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }
}
