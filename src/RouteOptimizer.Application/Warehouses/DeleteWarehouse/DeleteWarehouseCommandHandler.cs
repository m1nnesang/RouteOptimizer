using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Warehouses.DeleteWarehouse;

public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand, Result>
{
    private readonly IWarehouseRepository _warehouseRepository;

    public DeleteWarehouseCommandHandler(IWarehouseRepository warehouseRepository) => _warehouseRepository = warehouseRepository;

    public async Task<Result> Handle(DeleteWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(request.Id, ct);
        if (warehouse is null)
            return Result.Failure("Warehouse not found");

        await _warehouseRepository.DeleteAsync(warehouse, ct);

        return Result.Success();
    }
}
