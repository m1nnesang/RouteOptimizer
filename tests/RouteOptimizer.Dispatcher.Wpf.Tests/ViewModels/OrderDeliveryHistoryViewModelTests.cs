using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels;

public class OrderDeliveryHistoryViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();

    [Fact]
    public async Task LoadAsync_Success_PopulatesAttempts()
    {
        var orderId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<List<DeliveryAttemptItem>>($"api/orders/{orderId}/delivery-attempts", default))
            .ReturnsAsync([
                new DeliveryAttemptItem { Id = Guid.NewGuid(), Outcome = "Failed" },
                new DeliveryAttemptItem { Id = Guid.NewGuid(), Outcome = "Delivered" }
            ]);
        var vm = new OrderDeliveryHistoryViewModel(_api.Object);

        await vm.LoadAsync(orderId);

        vm.Attempts.Should().HaveCount(2);
        vm.HasError.Should().BeFalse();
        vm.IsLoading.Should().BeFalse();
        vm.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ResolvesPhotoUrl_OnlyForAttemptsWithKey()
    {
        var orderId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<List<DeliveryAttemptItem>>(It.IsAny<string>(), default))
            .ReturnsAsync([
                new DeliveryAttemptItem { Id = Guid.NewGuid(), Outcome = "Failed", PhotoKey = "key-1" },
                new DeliveryAttemptItem { Id = Guid.NewGuid(), Outcome = "Delivered" }
            ]);
        _api.Setup(a => a.GetAsync<string>("api/delivery-photos/key-1", default))
            .ReturnsAsync("https://minio.local/photo-1");
        var vm = new OrderDeliveryHistoryViewModel(_api.Object);

        await vm.LoadAsync(orderId);

        vm.Attempts.Single(a => a.PhotoKey == "key-1").PhotoUrl.Should().Be("https://minio.local/photo-1");
        vm.Attempts.Single(a => a.PhotoKey == null).PhotoUrl.Should().BeNull();
        _api.Verify(a => a.GetAsync<string>("api/delivery-photos/key-1", default), Times.Once);
        _api.Verify(a => a.GetAsync<string>(It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_PhotoResolutionFails_KeepsOtherAttempts()
    {
        var orderId = Guid.NewGuid();
        _api.Setup(a => a.GetAsync<List<DeliveryAttemptItem>>(It.IsAny<string>(), default))
            .ReturnsAsync([new DeliveryAttemptItem { Id = Guid.NewGuid(), Outcome = "Failed", PhotoKey = "key-1" }]);
        _api.Setup(a => a.GetAsync<string>(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new OrderDeliveryHistoryViewModel(_api.Object);

        await vm.LoadAsync(orderId);

        vm.Attempts.Should().HaveCount(1);
        vm.Attempts[0].PhotoUrl.Should().BeNull();
        vm.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<List<DeliveryAttemptItem>>(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new OrderDeliveryHistoryViewModel(_api.Object);

        await vm.LoadAsync(Guid.NewGuid());

        vm.ErrorMessage.Should().Be("Failed to load delivery history.");
        vm.HasError.Should().BeTrue();
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_NoAttempts_IsEmptyTrue()
    {
        _api.Setup(a => a.GetAsync<List<DeliveryAttemptItem>>(It.IsAny<string>(), default))
            .ReturnsAsync([]);
        var vm = new OrderDeliveryHistoryViewModel(_api.Object);

        await vm.LoadAsync(Guid.NewGuid());

        vm.Attempts.Should().BeEmpty();
        vm.IsEmpty.Should().BeTrue();
    }
}
