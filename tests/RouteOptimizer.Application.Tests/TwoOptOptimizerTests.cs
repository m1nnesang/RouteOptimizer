using FluentAssertions;
using RouteOptimizer.Application.Algorithms;
using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Application.Tests;

public class TwoOptOptimizerTests
{
    [Fact]
    public async Task OptimizeAsync_EmptyStops_ReturnsEmptyResult_ReturnsCorrectName()
    {
        var opt = new TwoOptOptimizer();
        var input = CreateInput(CreateMatrix(new double[0, 0]));

        var result = await opt.OptimizeAsync(input);

        result.OrderedStopIds.Should().BeEmpty();
        result.TotalDurationSeconds.Should().Be(0);
        result.AlgorithmName.Should().Be("Two Opt");
    }


    [Fact]
    public async Task OptimizeAsync_PicksBestOrder_ReturnsCorrectOrder()
    {
        var s0 = Guid.NewGuid();
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();

        var durations = new double[,]
        {
            //  depot  s0    s1    s2
            {   0,     15,   10,   2  },  // depot
            {   10,    0,    17,   2  },  // s0
            {   1,     19,    0,   4  },  // s1
            {   5,     2,    3,    0  },  // s2
        };

        var input = CreateInput(CreateMatrix(durations), s0, s1, s2);
        var opt = new TwoOptOptimizer();

        var result = await opt.OptimizeAsync(input);

        result.OrderedStopIds.Should().ContainInOrder(s0, s2, s1);
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
