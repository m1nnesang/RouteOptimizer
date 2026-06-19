using FluentAssertions;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels.Dialogs;

public class VehicleEditDialogViewModelTests
{
    [Fact]
    public void Constructor_CreateMode_DefaultsAreSet()
    {
        var vm = new VehicleEditDialogViewModel();

        vm.IsEditMode.Should().BeFalse();
        vm.Title.Should().Be("Create Vehicle");
        vm.LicenseCategories.Should().HaveCount(5);
        vm.SelectedLicenseCategory.Should().Be("B");
        vm.CargoTypes.Should().HaveCount(4);
        vm.CargoTypes.Should().AllSatisfy(c => c.IsSelected.Should().BeFalse());
    }

    [Fact]
    public void Constructor_EditMode_PopulatesFromExisting()
    {
        var existing = new VehicleListItem
        {
            Id = Guid.NewGuid(),
            Type = "Van",
            MaxWeightKg = 500,
            MaxVolumeM3 = 3.5m,
            LicenseCategory = "C",
            AllowedCargoTypes = ["General", "Fragile"]
        };
        var vm = new VehicleEditDialogViewModel(existing);

        vm.IsEditMode.Should().BeTrue();
        vm.Title.Should().Be("Edit Vehicle");
        vm.Type.Should().Be("Van");
        vm.MaxWeightKg.Should().Be(500);
        vm.MaxVolumeM3.Should().Be(3.5m);
        vm.SelectedLicenseCategory.Should().Be("C");
        vm.CargoTypes.Where(c => c.IsSelected).Select(c => c.Name)
            .Should().BeEquivalentTo(["General", "Fragile"]);
    }

    [Fact]
    public void CanConfirm_EmptyType_False()
    {
        var vm = new VehicleEditDialogViewModel();
        vm.Type = string.Empty;
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_WhitespaceType_False()
    {
        var vm = new VehicleEditDialogViewModel();
        vm.Type = "   ";
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_ValidType_True()
    {
        var vm = new VehicleEditDialogViewModel();
        vm.Type = "Truck";
        vm.CanConfirm.Should().BeTrue();
    }

    [Fact]
    public void BuildCreateRequest_IncludesSelectedCargoTypes()
    {
        var warehouseId = Guid.NewGuid();
        var vm = new VehicleEditDialogViewModel();
        vm.Type = "Van";
        vm.MaxWeightKg = 1000;
        vm.MaxVolumeM3 = 5m;
        vm.SelectedLicenseCategory = "BE";
        vm.CargoTypes.First(c => c.Name == "General").IsSelected = true;
        vm.CargoTypes.First(c => c.Name == "Refrigerated").IsSelected = true;

        var req = vm.BuildCreateRequest(warehouseId);

        req.WarehouseId.Should().Be(warehouseId);
        req.Type.Should().Be("Van");
        req.MaxWeightKg.Should().Be(1000);
        req.LicenseCategory.Should().Be("BE");
        req.AllowedCargoTypes.Should().BeEquivalentTo(["General", "Refrigerated"]);
    }

    [Fact]
    public void BuildUpdateRequest_MapsCorrectly()
    {
        var existing = new VehicleListItem
        {
            Type = "Truck", MaxWeightKg = 2000, MaxVolumeM3 = 10,
            LicenseCategory = "CE", AllowedCargoTypes = ["Hazardous"]
        };
        var vm = new VehicleEditDialogViewModel(existing);
        vm.Type = "MegaTruck";

        var req = vm.BuildUpdateRequest();

        req.Type.Should().Be("MegaTruck");
        req.AllowedCargoTypes.Should().BeEquivalentTo(["Hazardous"]);
    }
}
