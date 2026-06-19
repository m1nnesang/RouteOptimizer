using FluentAssertions;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels.Dialogs;

public class WarehouseEditDialogViewModelTests
{
    [Fact]
    public void Constructor_CreateMode_EmptyFields()
    {
        var vm = new WarehouseEditDialogViewModel();

        vm.IsEditMode.Should().BeFalse();
        vm.Title.Should().Be("Create Warehouse");
        vm.Name.Should().BeEmpty();
        vm.City.Should().BeEmpty();
        vm.Street.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_EditMode_PopulatesFromExisting()
    {
        var existing = new WarehouseListItem
        {
            Id = Guid.NewGuid(), Name = "WH Central",
            City = "Warszawa", Street = "ul. Magazynowa 1"
        };
        var vm = new WarehouseEditDialogViewModel(existing);

        vm.IsEditMode.Should().BeTrue();
        vm.Title.Should().Be("Edit Warehouse");
        vm.Name.Should().Be("WH Central");
        vm.City.Should().Be("Warszawa");
        vm.Street.Should().Be("ul. Magazynowa 1");
    }

    [Fact]
    public void CanConfirm_AllFieldsEmpty_False()
    {
        var vm = new WarehouseEditDialogViewModel();
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_MissingName_False()
    {
        var vm = new WarehouseEditDialogViewModel();
        vm.City = "Warszawa";
        vm.Street = "ul. A 1";
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_MissingCity_False()
    {
        var vm = new WarehouseEditDialogViewModel();
        vm.Name = "WH";
        vm.Street = "ul. A 1";
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_MissingStreet_False()
    {
        var vm = new WarehouseEditDialogViewModel();
        vm.Name = "WH";
        vm.City = "Warszawa";
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_AllRequired_True()
    {
        var vm = new WarehouseEditDialogViewModel();
        vm.Name = "WH";
        vm.City = "Warszawa";
        vm.Street = "ul. A 1";
        vm.CanConfirm.Should().BeTrue();
    }

    [Fact]
    public void BuildCreateRequest_MapsAllFields()
    {
        var vm = new WarehouseEditDialogViewModel();
        vm.Name = "Central";
        vm.City = "Kraków";
        vm.Street = "ul. B 2";
        vm.PostalCode = "30-001";
        vm.Country = "PL";

        var req = vm.BuildCreateRequest();

        req.Name.Should().Be("Central");
        req.City.Should().Be("Kraków");
        req.Street.Should().Be("ul. B 2");
        req.PostalCode.Should().Be("30-001");
        req.Country.Should().Be("PL");
    }

    [Fact]
    public void BuildUpdateRequest_MapsAllFields()
    {
        var existing = new WarehouseListItem { Name = "Old", City = "Old City", Street = "Old St" };
        var vm = new WarehouseEditDialogViewModel(existing);
        vm.Name = "New";
        vm.City = "New City";
        vm.Street = "New St";
        vm.PostalCode = "00-999";
        vm.Country = "PL";

        var req = vm.BuildUpdateRequest();

        req.Name.Should().Be("New");
        req.City.Should().Be("New City");
        req.Street.Should().Be("New St");
    }

    [Fact]
    public void BuildCreateRequest_ArgOrder_NameThenCityThenStreet()
    {
        var vm = new WarehouseEditDialogViewModel();
        vm.Name = "N"; vm.City = "C"; vm.Street = "S";

        var createReq = vm.BuildCreateRequest();
        var updateReq = vm.BuildUpdateRequest();

        createReq.Name.Should().Be("N");
        createReq.City.Should().Be("C");
        createReq.Street.Should().Be("S");

        updateReq.Name.Should().Be("N");
        updateReq.City.Should().Be("C");
        updateReq.Street.Should().Be("S");
    }
}
