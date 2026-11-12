using KG.MES.Shared.Events;
using KG.MES.Shared.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services;
using KG.MES.UI.Shared.Interfaces;
using Microsoft.AspNetCore.Components;

namespace KG.MES.UI.Shared.Components.Widgets;

public partial class OrderCommerceWidget : ComponentBase, ISavableWidget
{
	[Parameter] public Guid OrderId { get; set; }
	[Parameter] public bool CanEdit { get; set; }

	[Inject] private ProductionApiService ApiService { get; set; } = null!;
	[Inject] private IEventAggregator EventAggregator { get; set; } = null!;

	private OrderCommercialDto? commercial;
	private OrderCommercialDto? backupCommercial;

	private bool isLoading = true;
	private bool EditMode;
	private bool isSaving;
	private string? saveError;

	private Guid? editCustomerId;
	private string? editCustomerName;
	private decimal? editAmount;
	private string? editCurrency;

	private string customerSearch = string.Empty;
	private List<CustomerDto> filteredCustomers = [];
	private bool showCustomerDropdown;

	public bool HasUnsavedChanges() =>
		EditMode && (
			editCustomerId != commercial?.CustomerId ||
			editCustomerName != commercial?.CustomerName ||
			editAmount != commercial?.Amount ||
			editCurrency != (commercial?.Currency ?? "RUB"));

	public async Task SaveAllAsync() => await SaveChanges();

	protected override async Task OnInitializedAsync()
	{
		await LoadCommercial();
		isLoading = false;
	}

	private async Task LoadCommercial()
	{
		commercial = await ApiService.GetCommerceAsync(OrderId);
	}

	private void EnterEditMode()
	{
		EditMode = true;
		saveError = null;

		editCustomerId = commercial?.CustomerId;
		editCustomerName = commercial?.CustomerName;
		editAmount = commercial?.Amount;
		editCurrency = commercial?.Currency ?? "RUB";

		customerSearch = editCustomerName ?? string.Empty;
		filteredCustomers = [];
		showCustomerDropdown = false;
	}

	private void CancelEdit()
	{
		EditMode = false;
		saveError = null;
		filteredCustomers = [];
		showCustomerDropdown = false;
	}

	private async Task SaveChanges()
	{
		isSaving = true;
		saveError = null;

		try
		{
			var dto = new OrderCommercialRequestDto
			{
				ManagerId = commercial?.ManagerId,
				ManagerName = commercial?.ManagerName,
				CustomerId = editCustomerId,
				CustomerName = editCustomerName,
				Amount = editAmount,
				Currency = string.IsNullOrWhiteSpace(editCurrency) ? "RUB" : editCurrency
			};

			var result = await ApiService.UpdateOrderCommercialAsync(OrderId, dto);
			if (result == null)
			{
				saveError = "Не удалось сохранить";
				return;
			}

			commercial = result;
			EditMode = false;
			filteredCustomers = [];
			showCustomerDropdown = false;

			EventAggregator.Publish(new OrderUpdatedEvent
			{
				OrderId = OrderId,
				Source = "commercial"
			});
		}
		catch (Exception ex)
		{
			saveError = $"Ошибка: {ex.Message}";
		}
		finally
		{
			isSaving = false;
			StateHasChanged();
		}
	}

	private async Task OnCustomerSearchChanged()
	{
		editCustomerId = null;
		editCustomerName = customerSearch;

		if (customerSearch.Length < 2)
		{
			filteredCustomers = [];
			showCustomerDropdown = false;
			return;
		}

		filteredCustomers = await ApiService.GetCustomersAsync(customerSearch);
		showCustomerDropdown = filteredCustomers.Any();
	}

	private void SelectCustomer(CustomerDto customer)
	{
		editCustomerId = customer.Id;
		editCustomerName = customer.Name;
		customerSearch = customer.Name;
		filteredCustomers = [];
		showCustomerDropdown = false;
	}

	private void ClearCustomer()
	{
		editCustomerId = null;
		editCustomerName = null;
		customerSearch = string.Empty;
		filteredCustomers = [];
		showCustomerDropdown = false;
	}
}