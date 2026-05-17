using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Orders.CreateIndividualOrder;

public class CreateIndividualOrderCommandHandler : IRequestHandler<CreateIndividualOrderCommand, Result<Guid>>
{
    private readonly IGeocodingService _geocodingService;
    private readonly IOrderRepository _orderRepository;
    
    public CreateIndividualOrderCommandHandler(IGeocodingService geocodingService, IOrderRepository orderRepository)
    {
        _geocodingService = geocodingService;
        _orderRepository = orderRepository;
    }
    
    
    public async Task<Result<Guid>> Handle(CreateIndividualOrderCommand request, CancellationToken ct)
    {
        var address = Address.Create(request.Street, request.City, request.Postcode, request.Country);
        if (address.IsFailure) return Result<Guid>.Failure(address.Error ?? "Address is invalid");
        
        var geocodeResult = await _geocodingService.GeocodeAsync(address.Value!, ct);
        if (geocodeResult.IsFailure) return Result<Guid>.Failure(geocodeResult.Error ?? "Geocoding failed");
        
        var weight = Weight.Create(request.Weight);
        if (weight.IsFailure) return Result<Guid>.Failure(weight.Error ?? "Invalid weight");

        var volume = Volume.Create(request.Volume);
        if (volume.IsFailure) return Result<Guid>.Failure(volume.Error ?? "Invalid volume");

        var phone = PhoneNumber.Create(request.PhoneNumber);
        if (phone.IsFailure) return Result<Guid>.Failure(phone.Error ?? "Invalid phone");
        
      
        if (!Enum.TryParse<CargoType>(request.CargoType, out var cargoType))
            return Result<Guid>.Failure("Invalid cargo type");
      
        
        
        DeliveryWindow? window = null;
        #region DeliveryWindowLogic
        if (request.Start.HasValue && request.End.HasValue)
        {
            if (!Enum.TryParse<WindowStrictness>(request.WindowStrictness, out var windowStrictness)) 
                return Result<Guid>.Failure("Invalid window strictness");

            window = DeliveryWindow.Between(request.Start.Value, request.End.Value, windowStrictness);
        }

        else if (request.Start.HasValue)
        {
            if (!Enum.TryParse<WindowStrictness>(request.WindowStrictness, out var windowStrictness)) 
                return Result<Guid>.Failure("Invalid window strictness");
            
            window = DeliveryWindow.From(request.Start.Value, windowStrictness);
        }

        else if (request.End.HasValue)
        {
            if (!Enum.TryParse<WindowStrictness>(request.WindowStrictness, out var windowStrictness)) 
                return Result<Guid>.Failure("Invalid window strictness");
            
            window = DeliveryWindow.Until(request.End.Value, windowStrictness);
        }
        #endregion
        
        var order = IndividualOrder.Create(
            request.WarehouseId,
            address.Value!,
            geocodeResult.Value!,
            weight.Value!,
            volume.Value!,
            window ?? DeliveryWindow.AnyTime(),
            phone.Value!,
            cargoType,
            request.Notes,
            request.CustomerName,
            request.AllowLeaveAtDoor
        );

        if (order.IsFailure) return Result<Guid>.Failure(order.Error ?? "Failed to create order");

        await _orderRepository.AddAsync(order.Value!, ct);
        return Result<Guid>.Success(order.Value!.Id);
    }
}