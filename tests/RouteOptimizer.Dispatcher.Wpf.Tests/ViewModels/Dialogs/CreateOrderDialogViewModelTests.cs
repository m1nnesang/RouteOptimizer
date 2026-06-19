using FluentAssertions;
using Moq;
using RouteOptimizer.Dispatcher.Wpf.Models;
using RouteOptimizer.Dispatcher.Wpf.Services.Interfaces;
using RouteOptimizer.Dispatcher.Wpf.ViewModels.Dialogs;

namespace RouteOptimizer.Dispatcher.Wpf.Tests.ViewModels.Dialogs;

public class CreateOrderDialogViewModelTests
{
    private readonly WarehouseListItem _warehouse = new() { Id = Guid.NewGuid(), Name = "WH1" };

    private CreateOrderDialogViewModel CreateViewModel(IApiHttpClient? api = null) =>
        new([_warehouse], api);

    private static void FillCommonFields(CreateOrderDialogViewModel vm)
    {
        vm.Street = "ul. Testowa 1";
        vm.City = "Warszawa";
        vm.Postcode = "00-001";
        vm.Country = "PL";
        vm.PhoneNumber = "123456789";
        vm.WeightText = "1.5";
        vm.VolumeText = "0.5";
    }

    [Fact]
    public void Constructor_OrderTypeIsNull()
    {
        var vm = CreateViewModel();
        vm.OrderType.Should().BeNull();
        vm.IsTypeSelected.Should().BeFalse();
        vm.IsTypeNotSelected.Should().BeTrue();
    }

    [Fact]
    public void SelectBusiness_SetsOrderTypeAndProperties()
    {
        var vm = CreateViewModel();
        vm.SelectBusinessCommand.Execute(null);

        vm.OrderType.Should().Be(OrderKind.Business);
        vm.IsBusiness.Should().BeTrue();
        vm.IsIndividual.Should().BeFalse();
        vm.IsTypeSelected.Should().BeTrue();
        vm.Title.Should().Be("New business order");
    }

    [Fact]
    public void SelectIndividual_SetsOrderTypeAndProperties()
    {
        var vm = CreateViewModel();
        vm.SelectIndividualCommand.Execute(null);

        vm.OrderType.Should().Be(OrderKind.Individual);
        vm.IsIndividual.Should().BeTrue();
        vm.IsBusiness.Should().BeFalse();
        vm.Title.Should().Be("New individual order");
    }

    [Fact]
    public void Back_ClearsOrderType()
    {
        var vm = CreateViewModel();
        vm.SelectBusinessCommand.Execute(null);
        vm.BackCommand.Execute(null);

        vm.OrderType.Should().BeNull();
        vm.Title.Should().Be("New order");
    }

    [Fact]
    public void CanConfirm_NullOrderType_False()
    {
        var vm = CreateViewModel();
        FillCommonFields(vm);
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_NullWarehouse_False()
    {
        var vm = new CreateOrderDialogViewModel([], null);
        vm.SelectIndividualCommand.Execute(null);
        vm.SelectedWarehouse = null;
        FillCommonFields(vm);
        vm.CustomerName = "Jan";
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_MissingRequiredField_False()
    {
        var vm = CreateViewModel();
        vm.SelectIndividualCommand.Execute(null);
        FillCommonFields(vm);
        vm.CustomerName = "Jan";
        vm.Street = string.Empty;
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_InvalidWeight_False()
    {
        var vm = CreateViewModel();
        vm.SelectIndividualCommand.Execute(null);
        FillCommonFields(vm);
        vm.WeightText = "abc";
        vm.CustomerName = "Jan";
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_ZeroWeight_False()
    {
        var vm = CreateViewModel();
        vm.SelectIndividualCommand.Execute(null);
        FillCommonFields(vm);
        vm.WeightText = "0";
        vm.CustomerName = "Jan";
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_Individual_AllFieldsValid_True()
    {
        var vm = CreateViewModel();
        vm.SelectIndividualCommand.Execute(null);
        FillCommonFields(vm);
        vm.CustomerName = "Jan Kowalski";
        vm.CanConfirm.Should().BeTrue();
    }

    [Fact]
    public void CanConfirm_Individual_MissingCustomerName_False()
    {
        var vm = CreateViewModel();
        vm.SelectIndividualCommand.Execute(null);
        FillCommonFields(vm);
        vm.CustomerName = string.Empty;
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_Business_AllFieldsValid_True()
    {
        var vm = CreateViewModel();
        vm.SelectBusinessCommand.Execute(null);
        FillCommonFields(vm);
        vm.CompanyName = "ACME";
        vm.ContactPerson = "Jan";
        vm.StartText = "09:00";
        vm.EndText = "17:00";
        vm.CanConfirm.Should().BeTrue();
    }

    [Fact]
    public void CanConfirm_Business_MissingCompanyName_False()
    {
        var vm = CreateViewModel();
        vm.SelectBusinessCommand.Execute(null);
        FillCommonFields(vm);
        vm.ContactPerson = "Jan";
        vm.StartText = "09:00";
        vm.EndText = "17:00";
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_Business_EndBeforeStart_False()
    {
        var vm = CreateViewModel();
        vm.SelectBusinessCommand.Execute(null);
        FillCommonFields(vm);
        vm.CompanyName = "ACME";
        vm.ContactPerson = "Jan";
        vm.StartText = "17:00";
        vm.EndText = "09:00";
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_Business_EqualStartEnd_False()
    {
        var vm = CreateViewModel();
        vm.SelectBusinessCommand.Execute(null);
        FillCommonFields(vm);
        vm.CompanyName = "ACME";
        vm.ContactPerson = "Jan";
        vm.StartText = "09:00";
        vm.EndText = "09:00";
        vm.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public void SelectedSuggestion_FillsFields_AndSuppressesSearch()
    {
        var api = new Mock<IApiHttpClient>();
        var vm = new CreateOrderDialogViewModel([_warehouse], api.Object);

        vm.SelectedSuggestion = new AddressSuggestion(
            "Test label", "ul. Nowa 5", "Kraków", "30-001", "PL", 50.06, 19.94);

        vm.Street.Should().Be("ul. Nowa 5");
        vm.City.Should().Be("Kraków");
        vm.Postcode.Should().Be("30-001");
        vm.Country.Should().Be("PL");
        vm.IsSuggestionsOpen.Should().BeFalse();
        vm.AddressSuggestions.Should().BeEmpty();
        api.Verify(a => a.GetAsync<List<AddressSuggestion>>(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public void Street_ShortQuery_ClearsSuggestions()
    {
        var api = new Mock<IApiHttpClient>();
        var vm = new CreateOrderDialogViewModel([_warehouse], api.Object);

        vm.Street = "ab";

        vm.AddressSuggestions.Should().BeEmpty();
        vm.IsSuggestionsOpen.Should().BeFalse();
        api.Verify(a => a.GetAsync<List<AddressSuggestion>>(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public void Street_ManualChange_ClearsLatLng()
    {
        var api = new Mock<IApiHttpClient>();
        var vm = new CreateOrderDialogViewModel([_warehouse], api.Object);

        vm.SelectedSuggestion = new AddressSuggestion("L", "S", "C", "P", "PL", 52.0, 21.0);
        vm.Street = "nowy adres";

        var req = vm.BuildIndividualRequest();
        req.Latitude.Should().BeNull();
        req.Longitude.Should().BeNull();
    }

    [Fact]
    public void BuildIndividualRequest_MapsFieldsCorrectly()
    {
        var vm = CreateViewModel();
        vm.SelectIndividualCommand.Execute(null);
        vm.Street = " ul. Testowa 1 ";
        vm.City = " Warszawa ";
        vm.Postcode = "00-001";
        vm.Country = "PL";
        vm.PhoneNumber = " 123456789 ";
        vm.CustomerName = " Jan Kowalski ";
        vm.WeightText = "2.5";
        vm.VolumeText = "1.0";
        vm.Notes = "  ";

        var req = vm.BuildIndividualRequest();

        req.Street.Should().Be("ul. Testowa 1");
        req.City.Should().Be("Warszawa");
        req.PhoneNumber.Should().Be("123456789");
        req.CustomerName.Should().Be("Jan Kowalski");
        req.Weight.Should().Be(2.5m);
        req.Volume.Should().Be(1.0m);
        req.Notes.Should().BeNull();
    }

    [Fact]
    public void BuildIndividualRequest_WithoutTimeWindow_StrictnessNull()
    {
        var vm = CreateViewModel();
        vm.SelectIndividualCommand.Execute(null);
        FillCommonFields(vm);
        vm.CustomerName = "Jan";
        vm.StartText = string.Empty;
        vm.EndText = string.Empty;

        var req = vm.BuildIndividualRequest();

        req.Start.Should().BeNull();
        req.End.Should().BeNull();
        req.WindowStrictness.Should().BeNull();
    }

    [Fact]
    public void BuildBusinessRequest_MapsFieldsCorrectly()
    {
        var vm = CreateViewModel();
        vm.SelectBusinessCommand.Execute(null);
        FillCommonFields(vm);
        vm.CompanyName = " ACME Corp ";
        vm.ContactPerson = " Jan ";
        vm.StartText = "08:00";
        vm.EndText = "16:00";
        vm.Apartment = "  ";

        var req = vm.BuildBusinessRequest();

        req.CompanyName.Should().Be("ACME Corp");
        req.ContactPerson.Should().Be("Jan");
        req.Start.Should().Be(new TimeOnly(8, 0));
        req.End.Should().Be(new TimeOnly(16, 0));
        req.Apartment.Should().BeNull();
    }

    [Fact]
    public void SelectedSuggestion_LatLngStoredInRequest()
    {
        var vm = CreateViewModel();
        vm.SelectIndividualCommand.Execute(null);
        vm.SelectedSuggestion = new AddressSuggestion("L", "ul. Testowa 1", "Warszawa", "00-001", "PL", 52.23, 21.01);
        vm.PhoneNumber = "123456789";
        vm.WeightText = "1.5";
        vm.VolumeText = "0.5";
        vm.CustomerName = "Jan";

        var req = vm.BuildIndividualRequest();

        req.Latitude.Should().Be(52.23);
        req.Longitude.Should().Be(21.01);
    }
}
