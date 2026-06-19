using System.Net.Http;
using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels;

public class WarehousesViewModelTests
{
    private readonly Mock<IApiHttpClient> _api = new();
    private readonly Mock<IDialogService> _dialog = new();
    private readonly WarehouseListItem _warehouse = new() { Id = Guid.NewGuid(), Name = "WH1", City = "Warszawa", Street = "ul. Testowa 1" };

    private WarehousesViewModel CreateViewModel(List<WarehouseListItem>? warehouses = null)
    {
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ReturnsAsync(warehouses ?? [_warehouse]);
        return new WarehousesViewModel(_api.Object, _dialog.Object);
    }

    [Fact]
    public void Constructor_LoadsWarehouses()
    {
        var vm = CreateViewModel();
        vm.Warehouses.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadWarehouses_HttpError_SetsErrorMessage()
    {
        _api.Setup(a => a.GetAsync<List<WarehouseListItem>>("api/warehouses", default))
            .ThrowsAsync(new HttpRequestException());
        var vm = new WarehousesViewModel(_api.Object, _dialog.Object);

        vm.ErrorMessage.Should().Be("Failed to load warehouses. Please check your connection.");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void HasSelectedWarehouse_WhenSet_True()
    {
        var vm = CreateViewModel();
        vm.SelectedWarehouse = _warehouse;
        vm.HasSelectedWarehouse.Should().BeTrue();
    }

    [Fact]
    public async Task CreateWarehouse_DialogCancelled_SkipsPost()
    {
        var vm = CreateViewModel();
        _dialog.Setup(d => d.ShowWarehouseEditDialog(It.IsAny<WarehouseEditDialogViewModel>())).Returns(false);

        await vm.CreateWarehouseCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync<CreateWarehouseRequest, Guid>(It.IsAny<string>(), It.IsAny<CreateWarehouseRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateWarehouse_Confirmed_PostsAndReloads()
    {
        var vm = CreateViewModel();
        _dialog.Setup(d => d.ShowWarehouseEditDialog(It.IsAny<WarehouseEditDialogViewModel>()))
            .Callback<WarehouseEditDialogViewModel>(dvm => { dvm.Name = "New"; dvm.City = "City"; dvm.Street = "Street"; })
            .Returns(true);
        _api.Setup(a => a.PostAsync<CreateWarehouseRequest, Guid>("api/warehouses", It.IsAny<CreateWarehouseRequest>(), default))
            .ReturnsAsync(Guid.NewGuid());

        await vm.CreateWarehouseCommand.ExecuteAsync(null);

        _api.Verify(a => a.PostAsync<CreateWarehouseRequest, Guid>("api/warehouses", It.IsAny<CreateWarehouseRequest>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateWarehouse_PostThrows_SetsErrorMessage()
    {
        var vm = CreateViewModel();
        _dialog.Setup(d => d.ShowWarehouseEditDialog(It.IsAny<WarehouseEditDialogViewModel>()))
            .Callback<WarehouseEditDialogViewModel>(dvm => { dvm.Name = "N"; dvm.City = "C"; dvm.Street = "S"; })
            .Returns(true);
        _api.Setup(a => a.PostAsync<CreateWarehouseRequest, Guid>(It.IsAny<string>(), It.IsAny<CreateWarehouseRequest>(), default))
            .ThrowsAsync(new HttpRequestException());

        await vm.CreateWarehouseCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to create warehouse.");
    }

    [Fact]
    public async Task EditWarehouse_NoSelectedWarehouse_DoesNothing()
    {
        var vm = CreateViewModel();
        await vm.EditWarehouseCommand.ExecuteAsync(null);
        _dialog.Verify(d => d.ShowWarehouseEditDialog(It.IsAny<WarehouseEditDialogViewModel>()), Times.Never);
    }

    [Fact]
    public async Task EditWarehouse_Confirmed_PutsAndReloads()
    {
        var vm = CreateViewModel();
        vm.SelectedWarehouse = _warehouse;
        _dialog.Setup(d => d.ShowWarehouseEditDialog(It.IsAny<WarehouseEditDialogViewModel>()))
            .Callback<WarehouseEditDialogViewModel>(dvm => { dvm.Name = "Updated"; dvm.City = "C"; dvm.Street = "S"; })
            .Returns(true);

        await vm.EditWarehouseCommand.ExecuteAsync(null);

        _api.Verify(a => a.PutAsync($"api/warehouses/{_warehouse.Id}", It.IsAny<UpdateWarehouseRequest>(), default), Times.Once);
    }

    [Fact]
    public async Task EditWarehouse_PutThrows_SetsErrorMessage()
    {
        var vm = CreateViewModel();
        vm.SelectedWarehouse = _warehouse;
        _dialog.Setup(d => d.ShowWarehouseEditDialog(It.IsAny<WarehouseEditDialogViewModel>()))
            .Callback<WarehouseEditDialogViewModel>(dvm => { dvm.Name = "N"; dvm.City = "C"; dvm.Street = "S"; })
            .Returns(true);
        _api.Setup(a => a.PutAsync(It.IsAny<string>(), It.IsAny<UpdateWarehouseRequest>(), default))
            .ThrowsAsync(new HttpRequestException());

        await vm.EditWarehouseCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to update warehouse.");
    }

    [Fact]
    public async Task DeleteWarehouse_NoSelectedWarehouse_DoesNothing()
    {
        var vm = CreateViewModel();
        await vm.DeleteWarehouseCommand.ExecuteAsync(null);
        _dialog.Verify(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteWarehouse_ConfirmCancelled_SkipsDelete()
    {
        var vm = CreateViewModel();
        vm.SelectedWarehouse = _warehouse;
        _dialog.Setup(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        await vm.DeleteWarehouseCommand.ExecuteAsync(null);

        _api.Verify(a => a.DeleteAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task DeleteWarehouse_Confirmed_DeletesAndReloads()
    {
        var vm = CreateViewModel();
        vm.SelectedWarehouse = _warehouse;
        _dialog.Setup(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        await vm.DeleteWarehouseCommand.ExecuteAsync(null);

        _api.Verify(a => a.DeleteAsync($"api/warehouses/{_warehouse.Id}", default), Times.Once);
    }

    [Fact]
    public async Task DeleteWarehouse_DeleteThrows_SetsErrorMessage()
    {
        var vm = CreateViewModel();
        vm.SelectedWarehouse = _warehouse;
        _dialog.Setup(d => d.ShowConfirm(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _api.Setup(a => a.DeleteAsync(It.IsAny<string>(), default)).ThrowsAsync(new HttpRequestException());

        await vm.DeleteWarehouseCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Failed to delete warehouse.");
    }
}
