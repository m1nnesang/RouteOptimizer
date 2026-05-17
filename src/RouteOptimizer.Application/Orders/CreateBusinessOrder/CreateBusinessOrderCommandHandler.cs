using System.Xml;
using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Domain.Entities.Orders;
using RouteOptimizer.Domain.Enums;
using RouteOptimizer.Domain.ValueObjects;

namespace RouteOptimizer.Application.Orders.CreateBusinessOrder;

public class CreateBusinessOrderCommandHandler : IRequestHandler<CreateBusinessOrderCommand, Result<Guid>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IGeocodingService _geocodingService;
    
    public CreateBusinessOrderCommandHandler(IOrderRepository orderRepository, IGeocodingService geocodingService)
        => (_orderRepository, _geocodingService) = (orderRepository, geocodingService);

    public async Task<Result<Guid>> Handle(CreateBusinessOrderCommand request, CancellationToken ct)
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
        
        
        #region EnumParse
        if (!Enum.TryParse<CargoType>(request.CargoType, out var cargoType))
            return Result<Guid>.Failure("Invalid cargo type");

        if (!Enum.TryParse<WindowStrictness>(request.WindowStrictness, out var windowStrictness))
            return Result<Guid>.Failure("Invalid window strictness");
        #endregion

        DeliveryWindow window = DeliveryWindow.Between(request.Start, request.End, windowStrictness);

        var order = BusinessOrder.Create(
            request.WarehouseId,
            address.Value!,
            geocodeResult.Value!,
            weight.Value!,
            volume.Value!,
            window,
            phone.Value!,
            cargoType,
            request.Notes,
            request.CompanyName,
            request.ContactPerson
            
        );

        if (order.IsFailure) return Result<Guid>.Failure(order.Error ?? "Failed to create Order");

        await _orderRepository.AddAsync(order.Value!, ct);
        return Result<Guid>.Success(order.Value!.Id);
    }
}