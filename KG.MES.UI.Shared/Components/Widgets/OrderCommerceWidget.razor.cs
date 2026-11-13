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

	// === Автокомплит менеджера ===
	private Guid? editManagerId;
	private string? editManagerName;
	private string managerSearch = string.Empty;
	private List<SalesManagerDto> managerList = [];            // полный список менеджеров (для <select>-режима)
	private List<SalesManagerDto> filteredManagers = [];       // результаты поиска по всем сотрудникам
	private bool isManagerSearchMode;                          // true — пользователь печатает
	private bool showManagerDropdown;

	public bool HasUnsavedChanges() =>
		EditMode && (
			editManagerId != commercial?.ManagerId ||
			editManagerName != commercial?.ManagerName ||
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

	private async Task EnterEditMode()
	{
		EditMode = true;
		saveError = null;

		editManagerId = commercial?.ManagerId;
		editManagerName = commercial?.ManagerName;
		editCustomerId = commercial?.CustomerId;
		editCustomerName = commercial?.CustomerName;
		editAmount = commercial?.Amount;
		editCurrency = commercial?.Currency ?? "RUB";

		customerSearch = editCustomerName ?? string.Empty;
		managerSearch = editManagerName ?? string.Empty;

		filteredCustomers = [];
		showCustomerDropdown = false;
		filteredManagers = [];
		isManagerSearchMode = false;
		showManagerDropdown = false;

		await EnsureManagersLoadedAsync();
		StateHasChanged();
	}

	private void CancelEdit()
	{
		EditMode = false;
		saveError = null;
		filteredCustomers = [];
		showCustomerDropdown = false;
		filteredManagers = [];
		isManagerSearchMode = false;
		showManagerDropdown = false;
	}
	
	private async Task SaveChanges()
	{
		isSaving = true;
		saveError = null;

		try
		{
			var dto = new OrderCommercialRequestDto
			{
				ManagerId = editManagerId,
				ManagerName = editManagerName,
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

	private async Task EnsureManagersLoadedAsync()
	{
		if (managerList.Count > 0) return;
		managerList = await ApiService.GetManagersAsync();
	}

	// ========== АВТОКОМПЛИТ МЕНЕДЖЕРА ==========

	private void ToggleManagerDropdown()
	{
		showManagerDropdown = !showManagerDropdown;
		if (showManagerDropdown)
		{
			isManagerSearchMode = false;
			filteredManagers = [];
		}
	}

	private async Task OnManagerSearchChanged()
	{
		isManagerSearchMode = true;
		editManagerId = null;
		editManagerName = managerSearch;

		if (managerSearch.Length < 2)
		{
			filteredManagers = [];
			showManagerDropdown = false;
			return;
		}

		filteredManagers = await ApiService.SearchUsersAsync(managerSearch);
		showManagerDropdown = filteredManagers.Any();
	}

	private void SelectManager(SalesManagerDto manager)
	{
		editManagerId = manager.Id;
		editManagerName = manager.Name;
		managerSearch = manager.Name;
		filteredManagers = [];
		showManagerDropdown = false;
		isManagerSearchMode = false;
	}

	private void ClearManager()
	{
		editManagerId = null;
		editManagerName = null;
		managerSearch = string.Empty;
		filteredManagers = [];
		showManagerDropdown = false;
		isManagerSearchMode = false;
	}
}