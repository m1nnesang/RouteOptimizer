using FluentAssertions;
using Moq;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Exceptions;
using RouteOptimizer.Application.Models;
using RouteOptimizer.Application.Routes.Optimize;
using RouteOptimizer.Domain.Entities;
using RouteOptimizer.Domain.Entities.Route;
using RouteOptimizer.Domain.ValueObjects;
using IRouteOptimizerService = RouteOptimizer.Application.Abstractions.IRouteOptimizer;

namespace RouteOptimizer.Application.Tests.Optimization;

public class OptimizeRouteCommandHandlerTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IDistanceMatrixProvider> _distanceMatrixProvider = new();
    private readonly Mock<IRouteOptimizerService> _optimizer = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly OptimizeRouteCommandHandler _handler;

    public OptimizeRouteCommandHandlerTests()
    {
        _handler = new OptimizeRouteCommandHandler(
            _routeRepository.Object,
            _warehouseRepository.Object,
            _distanceMatrixProvider.Object,
            new[] { _optimizer.Object },
            _unitOfWork.Object);
    }

    #region Failure Cases

    [Fact]
    public async Task Handle_RouteNotFound_ThrowsNotFoundException()
    {
        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)null);

        var command = new OptimizeRouteCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));

        var act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Route not found*");
    }

    [Fact]
    public async Task Handle_WarehouseNotFound_ThrowsNotFoundException()
    {
        var route = Route.Create(Guid.NewGuid()).Value!;

        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)route);
        _warehouseRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var command = new OptimizeRouteCommand(route.Id, DateOnly.FromDateTime(DateTime.Today));

        var act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Warehouse not found*");
    }

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidRouteWithStops_ReturnsOptimizeRouteResult()
    {
        var warehouseId = Guid.NewGuid();
        var route = Route.Create(warehouseId).Value!;
        var stop = CreateStop(route.Id);
        route.AddStop(stop);

        var warehouse = CreateWarehouse(warehouseId);
        var matrix = new DistanceMatrix(new double[,] { { 0, 1 }, { 1, 0 } });

        var orderedIds = new List<Guid> { stop.Id }.AsReadOnly();
        var optimizerOutput = new RouteOptimizerOutput
        {
            OrderedStopIds = orderedIds,
            TotalDurationSeconds = 120,
            AlgorithmName = "TestAlgorithm"
        };

        _routeRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Route?)route);
        _warehouseRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)warehouse);
        _distanceMatrixProvider
            .Setup(x => x.GetMatrixAsync(
                It.IsAny<(double, double)>(),
                It.IsAny<IReadOnlyList<(double, double)>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(matrix);
        _optimizer
            .Setup(x => x.OptimizeAsync(It.IsAny<RouteOptimizerInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizerOutput);
        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new OptimizeRouteCommand(route.Id, DateOnly.FromDateTime(DateTime.Today));

        var result = await _handler.Handle(command, default);

        result.Should().NotBeNull();
        result.RouteId.Should().Be(route.Id);
        result.OrderedStopIds.Should().BeEquivalentTo(orderedIds);
        result.Comparisons.Should().HaveCount(1);
        result.Comparisons[0].AlgorithmName.Should().Be("TestAlgorithm");
    }

    #endregion

    private static Stop CreateStop(Guid routeId)
    {
        var address = Address.Create("ul. Marszałkowska 1", "Warszawa", "00-001", "Poland").Value!;
        var location = GeoCoordinate.Create(52.2297, 21.0122).Value!;
        return Stop.Create(routeId, address, location, null, 0, new List<Guid>()).Value!;
    }

    private static Warehouse CreateWarehouse(Guid id)
    {
        var address = Address.Create("ul. Magazynowa 1", "Warszawa", "02-274", "Poland").Value!;
        var location = GeoCoordinate.Create(52.1800, 20.9700).Value!;
        return Warehouse.Create("Magazyn Główny", address, location).Value!;
    }
}
