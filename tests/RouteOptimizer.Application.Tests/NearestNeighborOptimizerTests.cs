using FluentAssertions;
using RouteOptimizer.Application.Algorithms;
using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Application.Tests;

public class NearestNeighborOptimizerTests
{
    [Fact]
    public async Task OptimizeAsync_EmptyStops_ReturnsEmptyResult()
    {
        var opt = new NearestNeighborOptimizer();
        var input = CreateInput(CreateMatrix(new double[0, 0]));

        var result = await opt.OptimizeAsync(input);

        result.OrderedStopIds.Should().BeEmpty();
        result.TotalDurationSeconds.Should().Be(0);
        result.AlgorithmName.Should().Be("Nearest Neighbor");
    }

    [Fact]
    public async Task OptimizeAsync_PicksNearestStopAtEachStep_ReturnsCorrectOrder()
    {
        var s0 = Guid.NewGuid();
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();

        var durations = new double[,]
        {
            //  depot  s0    s1    s2
            {   0,     10,   1,    5  },  // depot
            {   10,    0,    8,    2  },  // s0
            {   1,     8,    0,    3  },  // s1
            {   5,     2,    3,    0  },  // s2
        };

        var input = CreateInput(CreateMatrix(durations), s0, s1, s2);
        var opt = new NearestNeighborOptimizer();

        var result = await opt.OptimizeAsync(input);

        result.OrderedStopIds.Should().ContainInOrder(s1, s2, s0);
    }

    [Fact]
    public async Task OptimizeAsync_PicksNearestStopAtEachStep_ReturnsCorrectTotalDuration()
    {
        var s0 = Guid.NewGuid();

        var duration = new double[,]
        {
            //  depot  s0
            { 0, 5 }, // depot
            { 7, 0 }, // s0
        };

        var input = CreateInput(CreateMatrix(duration), s0);
        var opt = new NearestNeighborOptimizer();

        var result = await opt.OptimizeAsync(input);

        result.TotalDurationSeconds.Should().Be(12);
    }


    private static RouteOptimizerInput CreateInput(DistanceMatrix matrix, params Guid[] stopIds)
    {
        var stops = stopIds
            .Select((id, i) => new StopInput(id, 0, 0, null))
            .ToList();

        return new RouteOptimizerInput
        {
            Warehouse = (0, 0),
            Stops = stops,
            Matrix = matrix,
            RouteDate = DateOnly.FromDateTime(DateTime.Today)
        };
    }

    private static DistanceMatrix CreateMatrix(double[,] durations) => new(durations);
}
