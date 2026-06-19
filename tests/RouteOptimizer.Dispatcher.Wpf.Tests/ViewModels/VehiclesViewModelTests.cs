using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.VIewModels;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels;

public class VehiclesViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();
    private readonly Mock<IDialogService> _dialog = new();
    private readonly WarehouseListItem _warehouse = new() { Id = Guid.NewGuid(), Name = "WH1" };

    private VehiclesViewModel CreateViewModel(List<WarehouseListItem>? warehouses = null)
    {
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync(warehouses ?? [_warehouse]);
        _api.Setup(a => a.GetAsync<List<VehicleListItem>>(It.IsAny<string>(), default))
            .ReturnsAsync([]);
        return new VehiclesViewModel(_api.Object, _dialog.Object);
    }

    [Fact]
    public void Constructor_LoadsWarehousesAndSelectsFirst()
    {
        var vm = CreateViewModel();
        vm.Warehouses.Should().HaveCount(1);
        vm.SelectedWarehouse.Should().Be(_warehouse);
    }

    [Fact]
    public void Constructor_NoWarehouses_SelectedWarehouseIsNull()
    {
        var vm = CreateViewModel([]);
        vm.SelectedWarehouse.Should().BeNull();
        vm.HasSelectedWarehouse.Should().BeFalse();
    }

    [Fact]
    public async Task LoadWarehouses_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new VehiclesViewModel(_api.Object, _dialog.Object);

        vm.ErrorMessage.Should().Be("Failed to load warehouses. Please check your connection.");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void SelectedWarehouse_Changed_ClearsVehiclesAndReloads()
    {
        var vm = CreateViewModel();
        var vehicle = new VehicleListItem { Id = Guid.NewGuid(), Type = "Van" };
        vm.Vehicles.Add(vehicle);
        vm.SelectedVehicle = vehicle;

        var newWarehouse = new WarehouseListItem { Id = Guid.NewGuid(), Name = "WH2" };
        _api.Setup(a => a.GetAsync<List<VehicleListItem>>($"api/vehicles?warehouseId={newWarehouse.Id}", default))
            .ReturnsAsync([]);
        vm.SelectedWarehouse = newWarehouse;

        vm.SelectedVehicle.Should().BeNull();
    }

    [Fact]
    public void SelectedWarehouse_SetToNull_DoesNotLoadVehicles()
    {
        var vm = CreateViewModel();
        vm.SelectedWarehouse = null;
        _api.Verify(a => a.GetAsync<List<VehicleListItem>>(It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task LoadVehicles_UrlContainsWarehouseId()
    {
        string? capturedUrl = null;
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([_warehouse]);
        _api.Setup(a => a.GetAsync<List<VehicleListItem>>(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync([]);
        var vm = new VehiclesViewModel(_api.Object, _dialog.Object);
        await Task.Yield();

        capturedUrl.Should().Contain($"warehouseId={_warehouse.Id}");
    }

    [Fact]
    public async Task LoadVehicles_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync([_warehouse]);
        _api.Setup(a => a.GetAsync<List<VehicleListItem>>(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new VehiclesViewModel(_api.Object, _dialog.Object);

        vm.ErrorMessage.Should().Be("Failed to load vehicles.");
    }

    [Fact]
    public void HasSelectedWarehouse_TrueWhenSet()
    {
        var vm = CreateViewModel();
        vm.HasSelectedWarehouse.Should().BeTrue();
    }

    [Fact]
    public void HasSelectedVehicle_TrueWhenSet()
    {
        var vm = CreateViewModel();
        vm.SelectedVehicle = new VehicleListItem { Id = Guid.NewGuid() };
        vm.HasSelectedVehicle.Should().BeTrue();
    }

    [Fact]
    public async Task CreateVehicle_NoSelectedWarehouse_DoesNothing()
    {
        var vm = CreateViewModel([]);
        await vm.CreateVehicleCommand.ExecuteAsync(null);
        _dialog.Verify(d => d.ShowVehicleEditDialog(It.IsAny<VehicleEditDialogViewModel>()), Times.Never);
    }

    [Fact]
    public async Task CreateVehicle_DialogCancelled_SkipsPost()
    {
        var vm = CreateViewModel();
        _dialog.Setup(d => d.ShowVehicleEditDialog(It.IsAny<VehicleEditDialogViewModel>())).Returns(false);

        await vm.CreateVehicleCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync<CreateVehicleRequest, Guid>(It.IsAny<string>(), It.IsAny<CreateVehicleRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateVehicle_Confirmed_PostsAndReloads()
    {
        var vm = CreateViewModel();
        _dialog.Setup(d => d.ShowVehicleEditDialog(It.IsAny<VehicleEditDialogViewModel>()))
            .Callback<VehicleEditDialogViewModel>(dvm => dvm.Type = "Van")
            .Returns(true);
        _api.Setup(a => a.PostAsync<CreateVehicleRequest, Guid>("api/vehicles", It.IsAny<CreateVehicleRequest>(), default))
            .ReturnsAsync(Guid.NewGuid());

        await vm.CreateVehicleCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync<CreateVehicleRequest, Guid>("api/vehicles", It.IsAny<CreateVehicleRequest>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateVehicle_PostThrows_SetsErrorMessage()
    {
        var vm = CreateViewModel();
        _dialog.Setup(d => d.ShowVehicleEditDialog(It.IsAny<VehicleEditDialogViewModel>()))
            .Callback<VehicleEditDialogViewModel>(dvm => dvm.Type = "Van")
            .Returns(true);
        _api.Setup(a => a.PostAsync<CreateVehicleRequest, Guid>(It.IsAny<string>(), It.IsAny<CreateVehicleRequest>(), default))
            .ThrowsAsync(new HttpRequestException());

        await vm.CreateVehicleCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to create vehicle.");
    }

    [Fact]
    public async Task EditVehicle_NoSelectedVehicle_DoesNothing()
    {
        var vm = CreateViewModel();
        await vm.EditVehicleCommand.ExecuteAsync(null);
        _dialog.Verify(d => d.ShowVehicleEditDialog(It.IsAny<VehicleEditDialogViewModel>()), Times.Never);
    }

    [Fact]
    public async Task EditVehicle_Confirmed_PutsAndReloads()
    {
        var vehicleId = Guid.NewGuid();
        var vm = CreateViewModel();
        vm.SelectedVehicle = new VehicleListItem { Id = vehicleId, Type = "Truck" };
        _dialog.Setup(d => d.ShowVehicleEditDialog(It.IsAny<VehicleEditDialogViewModel>()))
            .Callback<VehicleEditDialogViewModel>(dvm => dvm.Type = "Van")
            .Returns(true);

        await vm.EditVehicleCommand.ExecuteAsync(null);

        _api.Verify(a => a.PutAsync($"api/vehicles/{vehicleId}", It.IsAny<UpdateVehicleRequest>(), default), Times.Once);
    }

    [Fact]
    public async Task EditVehicle_PutThrows_SetsErrorMessage()
    {
        var vm = CreateViewModel();
        vm.SelectedVehicle = new VehicleListItem { Id = Guid.NewGuid(), Type = "Van" };
        _dialog.Setup(d => d.ShowVehicleEditDialog(It.IsAny<VehicleEditDialogViewModel>()))
            .Callback<VehicleEditDialogViewModel>(dvm => dvm.Type = "Van")
            .Returns(true);
        _api.Setup(a => a.PutAsync(It.IsAny<string>(), It.IsAny<UpdateVehicleRequest>(), default))
            .ThrowsAsync(new HttpRequestException());

        await vm.EditVehicleCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to update vehicle.");
    }

    [Fact]
    public async Task DeleteVehicle_NoSelectedVehicle_DoesNothing()
    {
        var vm = CreateViewModel();
        await vm.DeleteVehicleCommand.ExecuteAsync(null);
        _dialog.Verify(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteVehicle_ConfirmCancelled_SkipsDelete()
    {
        var vm = CreateViewModel();
        vm.SelectedVehicle = new VehicleListItem { Id = Guid.NewGuid(), Type = "Van" };
        _dialog.Setup(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        await vm.DeleteVehicleCommand.ExecuteAsync(null);

        _api.Verify(a => a.DeleteAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task DeleteVehicle_Confirmed_DeletesAndReloads()
    {
        var vehicleId = Guid.NewGuid();
        var vm = CreateViewModel();
        vm.SelectedVehicle = new VehicleListItem { Id = vehicleId, Type = "Van" };
        _dialog.Setup(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        await vm.DeleteVehicleCommand.ExecuteAsync(null);

        _api.Verify(a => a.DeleteAsync($"api/vehicles/{vehicleId}", default), Times.Once);
    }

    [Fact]
    public async Task DeleteVehicle_DeleteThrows_SetsErrorMessage()
    {
        var vm = CreateViewModel();
        vm.SelectedVehicle = new VehicleListItem { Id = Guid.NewGuid(), Type = "Van" };
        _dialog.Setup(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _api.Setup(a => a.DeleteAsync(It.IsAny<string>(), default)).ThrowsAsync(new HttpRequestException());

        await vm.DeleteVehicleCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to delete vehicle.");
    }
}
