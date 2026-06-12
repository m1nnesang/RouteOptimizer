using FluentAssertions;
using RouteOptimizer.Application.Algorithms;
using RouteOptimizer.Application.Models;

namespace RouteOptimizer.Application.Tests.Optimization;

public class NearestNeighborOptimizerTests
{
    #region Tests

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
            {   0,     10,   1,    5  },
            {   10,    0,    8,    2  },
            {   1,     8,    0,    3  },
            {   5,     2,    3,    0  },
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
            { 0, 5 },
            { 7, 0 },
        };

        var input = CreateInput(CreateMatrix(duration), s0);
        var opt = new NearestNeighborOptimizer();

        var result = await opt.OptimizeAsync(input);

        result.TotalDurationSeconds.Should().Be(12);
    }


    [Fact]
    public async Task OptimizeAsync_MatrixSizeMismatch_ThrowsInvalidOperationException()
    {
        var s0 = Guid.NewGuid();
        var s1 = Guid.NewGuid();

        var durations = new double[,]
        {
            { 0, 5 },
            { 7, 0 }
        };

        var input = CreateInput(CreateMatrix(durations), s0, s1);
        var opt = new NearestNeighborOptimizer();

        var act = async () => await opt.OptimizeAsync(input);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Matrix size*");
    }

    [Fact]
    public async Task OptimizeAsync_SingleStop_ReturnsThatStopWithRoundTripDuration()
    {
        var s0 = Guid.NewGuid();

        var durations = new double[,]
        {
            { 0, 3 },
            { 4, 0 }
        };

        var input = CreateInput(CreateMatrix(durations), s0);
        var opt = new NearestNeighborOptimizer();

        var result = await opt.OptimizeAsync(input);

        result.OrderedStopIds.Should().ContainSingle().Which.Should().Be(s0);
        result.TotalDurationSeconds.Should().Be(7);
    }

    #endregion

    #region Helpers

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

    #endregion
}
