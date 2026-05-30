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


    [Fact]
    public async Task OptimizeAsync_MatrixSizeMismatch_ThrowsInvalidOperationException()
    {
        var s0 = Guid.NewGuid();
        var s1 = Guid.NewGuid();

        // matrix is 2x2 (size=2) but stops.Count+1 = 3 — mismatch
        var durations = new double[,]
        {
            { 0, 5 },
            { 7, 0 }
        };

        var input = CreateInput(CreateMatrix(durations), s0, s1);
        var opt = new TwoOptOptimizer();

        var act = async () => await opt.OptimizeAsync(input);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Matrix size*");
    }

    [Fact]
    public async Task OptimizeAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        var s0 = Guid.NewGuid();
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();

        var durations = new double[,]
        {
            //  depot  s0    s1    s2
            {   0,     10,   10,   10  },
            {   10,    0,    1,    10  },
            {   10,    1,    0,    10  },
            {   10,    10,   10,   0   }
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var input = CreateInput(CreateMatrix(durations), s0, s1, s2);
        var opt = new TwoOptOptimizer();

        var act = async () => await opt.OptimizeAsync(input, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
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
