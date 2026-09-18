using Microsoft.AspNetCore.Components;
using KG.MES.Shared.Models.Config;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services;
using KG.MES.Shared.Helpers;
using System.Text.Json;
using KG.MES.Shared.Models;
using KG.MES.Shared.Events;
using KG.MES.Shared.Interfaces;
using KG.MES.Shared.Constants;
using System.Reflection;

namespace KG.MES.UI.Shared.Components;
public partial class OrderListView<TListItem, TCardItem> : ComponentBase
	where TListItem : class
	where TCardItem : class

{
	[Parameter] public OrderViewSettings Settings { get; set; } = new();
	[Parameter] public EventCallback<TListItem> OnOrderClick { get; set; }
	[Parameter] public EventCallback<TListItem> OnEditOrder { get; set; }
	[Parameter] public EventCallback<TListItem> OnDeleteOrder { get; set; }
	[Parameter] public RenderFragment? HeaderActions { get; set; }
	[Parameter] public Func<Guid?, Guid[]?, string?, int, int, string?, string?, List<FilterCondition>, Task<PaginatedResponse<TListItem>>>? LoadItems { get; set; }
	[Parameter] public Func<Guid, Task<TCardItem>>? LoadItem { get; set; }


	[Inject] private ProductionApiService ApiService { get; set; } = null!;
	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;
	[Inject] private IEventAggregator EventAggregator { get; set; } = null!;
	[Inject] private NavigationManager NavManager { get; set; } = null!;
	[Inject] private UserSessionService Session { get; set; } = null!;


	private OrderDashboard<TCardItem>? dashboardRef;
	private string Endpoint => Settings.ListEndpoint;
	private string CardEndpoint => Settings.CardEndpoint;
	private string Title => Settings.Title;
	private bool ShowActions => Settings.ShowActions;
	private PaginatedResponse<TListItem> orders = new();
	private List<WorkplaceDto> workplaces = [];
	private List<ColumnInfo> columnInfos = [];
	private List<ColumnSetting> columnSettings = [];
	private bool isLoading = false;
	private string searchNumber = "";
	private int currentPage = 1;
	private int pageSize = 50;
	private string sortBy = "ready_date";
	private string sortOrder = "asc";
	private Guid? selectedWorkplaceId;
	private string tableKey => $"{Endpoint}_{typeof(TListItem).Name}";
	private bool isColumnsOpen = false;
	private bool useSplitView;
	private string savedPanelWidth = "66%";
	private DotNetObjectReference<OrderListView<TListItem, TCardItem>>? panelResizeRef;
	private string? lastReportedWidth;
	private bool _panelResizeInitialized = false;
	//private List<Guid> selectedWorkplaceIds = [];
	private bool dropdownOpen;
	private Guid[] selectedWorkplaceIds = [];
	private SavedFilter? savedFilter;
	private ElementReference pageContainer;
	private double? scrollYBeforeModal;
	private bool showAdvancedFilter;
	private Dictionary<string, HashSet<string>> selectedFilters = [];
	private HashSet<string> expandedGroups = [];
	private bool HasActiveFilters => selectedFilters.Values.Any(v => v.Count > 0);
	private List<ColumnInfo> filterableColumns => columnInfos.Where(c => c.Filterable).ToList();
	private Dictionary<string, List<FacetValueDto>> facets = [];
	private ITotalsDto? totals;


	private IconInfo testIcon = new IconInfo
	{
						Class = "bi bi-piggy-bank-fill",
						Css = "order-icon-econom",
						Title = "Эконом"
	};

	protected override async Task OnInitializedAsync()
	{
		columnInfos = ColumnHelper.GetColumns<TListItem>();

		await LoadSettings();
		workplaces = await ApiService.GetAllWorkplacesAsync();
		await LoadFacetsAsync();
		await LoadOrders();

		EventAggregator.Subscribe<OrderUpdatedEvent>(OnOrderUpdated);
	}

	private async void OnOrderUpdated(OrderUpdatedEvent e)
	{
		await LoadOrders();
		StateHasChanged();
	}

	private async Task LoadSettings()
	{
		try
		{
			var splitViewSetting = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "useSplitView");
			useSplitView = splitViewSetting != null && splitViewSetting.ToLower() == "true";

			var splitPanelWidth = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "splitPanelWidth");
			//if (!string.IsNullOrEmpty(splitPanelWidth))
			//	savedPanelWidth = splitPanelWidth;

			var orderFilter = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"orderFilter_{Endpoint}");
			if (!string.IsNullOrEmpty(orderFilter))
			{
				savedFilter = JsonSerializer.Deserialize<SavedFilter>(orderFilter,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				if (savedFilter != null)
				{
					selectedWorkplaceIds = savedFilter.WorkplaceIds ?? [];
					searchNumber = savedFilter.OrderNumber ?? "";
				}
			}

			var tableSettings = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"table_settings_{tableKey}");

			//TODO временно, чтобы у пользователей не слетели настройки UI. Спустя время надо убрать.
			if (string.IsNullOrEmpty(tableSettings))
			{
				// Пробуем старый ключ с Dto
				var oldTableKey = tableKey.Replace("ViewModel", "Dto");
				tableSettings = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"table_settings_{oldTableKey}");
			}

			columnSettings = TableSettingsManager.GetSettings<TListItem>(tableSettings);
		}
		catch
		{
			columnSettings = TableSettingsManager.GetDefaultSettings<TListItem>();
		}
	}

	private List<(ColumnInfo Info, ColumnSetting Setting)> BuildVisibleColumns()
	{
		return columnSettings
			.Where(s => s.Visible)
			.Join(columnInfos,
				s => s.PropertyName,
				i => i.PropertyName,
				(s, i) => (Info: i, Setting: s))
			.OrderBy(x => x.Setting.Order)
			.ToList();
	}

	private async Task SaveSettings()
	{
		var json = TableSettingsManager.Serialize(columnSettings);
		await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"table_settings_{tableKey}", json);
	}

	private async Task ToggleColumn(string propertyName, bool visible)
	{
		var setting = columnSettings.FirstOrDefault(s => s.PropertyName == propertyName);
		if (setting != null)
		{
			setting.Visible = visible;
			await SaveSettings();
			StateHasChanged();
		}
	}

	private async Task MoveColumn(string propertyName, int direction)
	{
		var setting = columnSettings.FirstOrDefault(s => s.PropertyName == propertyName);
		if (setting == null) return;

		var orderedList = columnSettings.OrderBy(s => s.Order).ToList();
		var currentIndex = orderedList.IndexOf(setting);
		var newIndex = currentIndex + direction;

		if (newIndex < 0 || newIndex >= orderedList.Count) return;

		// Меняем местами
		var swap = orderedList[newIndex];
		var tempOrder = setting.Order;
		setting.Order = swap.Order;
		swap.Order = tempOrder;

		// Нормализуем порядок
		orderedList = columnSettings.OrderBy(s => s.Order).ToList();
		for (int i = 0; i < orderedList.Count; i++)
			orderedList[i].Order = i;

		await SaveSettings();
		StateHasChanged();
	}

	private async Task ResetColumns()
	{
		columnSettings = TableSettingsManager.GetDefaultSettings<TListItem>();
		await SaveSettings();
		StateHasChanged();
	}

	private async Task LoadSortFromStorage()
	{
		try
		{
			var json = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"sort_{tableKey}");
			if (!string.IsNullOrEmpty(json))
			{
				var data = JsonSerializer.Deserialize<SortData>(json);
				sortBy = data?.SortBy ?? "ready_date";
				sortOrder = data?.Ascending == true ? "asc" : "desc";
			}
		}
		catch { }
	}
	
	private async Task LoadOrders()
	{
		isLoading = true;
		StateHasChanged();

		try
		{
			ApiService.Session = Session;

			var filters = BuildFilterConditionsForIn();

			if (LoadItems != null)
			{
				orders = await LoadItems(
					selectedWorkplaceId,
					selectedWorkplaceIds,
					string.IsNullOrEmpty(searchNumber) ? null : searchNumber,
					currentPage,
					pageSize,
					sortBy,
					sortOrder,
					filters
				);

				totals = orders.Totals;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading orders: {ex.Message}");
		}
		finally
		{
			isLoading = false;
			StateHasChanged();
		}
	}

	private async Task ApplyFilters()
	{
		currentPage = 1;
		await LoadOrders();
	}

	private async Task GoToPage(int page)
	{
		currentPage = page;
		await LoadOrders();
	}

	private async Task OnPageSizeChanged()
	{
		currentPage = 1;
		await LoadOrders();
	}

	private async Task OnSearchKeyUp(KeyboardEventArgs e)
	{
		if (e.Key == "Enter")
			await ApplyFilters();
	}

	private List<int> GetPageNumbers()
	{
		var pages = new List<int>();
		var start = Math.Max(1, orders.Page - 2);
		var end = Math.Min(orders.TotalPages, orders.Page + 2);

		for (int i = start; i <= end; i++)
			pages.Add(i);

		return pages;
	}

	private TListItem? selectedOrder;

	private bool isModalOpen;

	private async Task OpenOrder(TListItem order)
	{
		selectedOrder = order;
		if (useSplitView)
		{
			// Просто обновляем панель — Dashboard сам переинициализируется
			StateHasChanged();
		}
		else
		{
			// Сохраняем позицию скролла
			scrollYBeforeModal = await JSRuntime.InvokeAsync<double>("scrollFunctions.getScrollTop", pageContainer);

			isModalOpen = true;
			StateHasChanged();
		}
	}

	private async Task EditOrder(TListItem order)
	{
		Guid orderId = GetOrderId(order);

		if (orderId == Guid.Empty)
			return;

		NavManager.NavigateTo($"create-order?edit={orderId}");
		await OnEditOrder.InvokeAsync(order);
	}

	private async Task DeleteOrder(TListItem order)
	{
		Guid orderId = GetOrderId(order);

		if (orderId == Guid.Empty)
			return;

		var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", $"Удалить заказ {GetOrderNumber(order)}?");
		if (confirmed)
		{

			var success = await ApiService.DeleteOrderAsync(orderId);
			if (success) await LoadOrders();
		}
	}

	private async Task CloseOrder()
	{
		selectedOrder = default;
		isModalOpen = false;

		if (useSplitView)
		{
			// Без перезагрузки списка — просто скрываем панель
			dashboardRef = null;
			StateHasChanged();
		}
		else
		{
			await LoadOrders();
			StateHasChanged();

			// Восстанавливаем позицию скролла
			if (scrollYBeforeModal.HasValue)
			{
				// Восстанавливаем позицию
				await JSRuntime.InvokeVoidAsync("scrollFunctions.setScrollTop", pageContainer, scrollYBeforeModal.Value);
				// Сбрасываем переменную, чтобы не скроллить при обычных рендерах
				scrollYBeforeModal = null;
			}

		}
	}

	private Guid GetOrderId(TListItem order)
	{
		var prop = typeof(TListItem).GetProperty("Id");
		return (Guid)(prop?.GetValue(order) ?? Guid.Empty);
	}

	private string GetOrderNumber(TListItem order)
	{
		var prop = typeof(TListItem).GetProperty("OrderNumber");
		return prop?.GetValue(order)?.ToString() ?? "—";
	}

	private async Task ToggleViewMode()
	{
		useSplitView = !useSplitView;
		await JSRuntime.InvokeVoidAsync("localStorage.setItem", "useSplitView", useSplitView.ToString());

		StateHasChanged();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (useSplitView && !_panelResizeInitialized)
		{
			//await InitializePanelResize(); //TODO доработать сохранение измененной ширины таблицы (панель в этом месте еще не существует)
		}
	}

	[JSInvokable]
	public async Task OnPanelResized(double width)
	{
		var newWidth = $"{width}px";

		// Защита от спама одинаковых значений (ResizeObserver может вызывать callback очень часто)
		if (newWidth == lastReportedWidth) return;
		lastReportedWidth = newWidth;

		await InvokeAsync(async () =>
		{
			savedPanelWidth = newWidth;
			try
			{
				await JSRuntime.InvokeVoidAsync("localStorage.setItem", "splitPanelWidth", savedPanelWidth);
			}
			catch {}

			StateHasChanged();
		});
	}

	private async Task InitializePanelResize()
	{
		if (_panelResizeInitialized) return;

		try
		{
			panelResizeRef = DotNetObjectReference.Create(this);
			await JSRuntime.InvokeVoidAsync("panelResize.init", panelResizeRef, "split-panel");
			_panelResizeInitialized = true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to initialize panel resize: {ex.Message}");
			_panelResizeInitialized = false;
		}
	}

	private void ToggleDropdown()
	{
		dropdownOpen = !dropdownOpen;
	}

	private void CloseDropdown()
	{
		dropdownOpen = false;
	}

	private async Task ToggleWorkplace(Guid id, bool isChecked)
	{
		if (isChecked)
		{
			if (!selectedWorkplaceIds.Contains(id))
				selectedWorkplaceIds = selectedWorkplaceIds.Append(id).ToArray();
		}
		else
		{
			selectedWorkplaceIds = selectedWorkplaceIds.Where(x => x != id).ToArray();
		}

		//await ApplyFilters();
	}

	private async Task SaveFilter()
	{
		var filter = new SavedFilter
		{
			WorkplaceIds = selectedWorkplaceIds,
			OrderNumber = searchNumber
		};

		if (filter == savedFilter) return;

		savedFilter = filter;

		var json = JsonSerializer.Serialize(filter);
		await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"orderFilter_{Endpoint}", json);
	}

	private bool IsFilterSaved()
	{
		if (savedFilter == null) return false;

		if(selectedWorkplaceIds.Length == 0 && searchNumber == string.Empty)
			return false;

		return selectedWorkplaceIds.SequenceEqual(savedFilter.WorkplaceIds ?? [])
			&& searchNumber == (savedFilter.OrderNumber ?? string.Empty);
	}

	private async Task ClearFilter()
	{
		selectedWorkplaceIds = [];
		searchNumber = "";
		await ApplyFilters();
		StateHasChanged();
	}

	private async Task RestoreFilter()
	{
		if (savedFilter != null)
		{
			selectedWorkplaceIds = savedFilter.WorkplaceIds ?? [];
			searchNumber = savedFilter.OrderNumber ?? "";
			await ApplyFilters();
			StateHasChanged();
		}
	}

	private async Task HandleSortChanged((string SortBy, bool Ascending) sort)
	{
		sortBy = sort.SortBy;
		sortOrder = sort.Ascending ? "asc" : "desc";
		await LoadOrders();
	}

	// Фильтруемые поля из ColumnInfo
	//private Dictionary<string, List<string>> advancedFilterGroups => columnInfos
	//	.Where(c => c.Filterable)
	//	.ToDictionary(
	//		c => c.Title,
	//		c => orders.Data.Select(item =>
	//		{
	//			var prop = typeof(TListItem).GetProperty(c.PropertyName);
	//			var value = prop?.GetValue(item);
	//			return value switch
	//			{
	//				bool b => b ? "Да" : "Нет",
	//				null => "—",
	//				_ => value.ToString() ?? "—"
	//			};
	//		}).Distinct().ToList()
	//	);

	private bool HasAdvancedFilters => selectedFilters.Values.Any(v => v.Count > 0);
	private void ToggleAdvancedFilter() => showAdvancedFilter = !showAdvancedFilter;
	private void CloseAdvancedFilter() => showAdvancedFilter = false;

	private void ToggleAdvancedGroup(string group)
	{
		if (expandedGroups.Contains(group))
			expandedGroups.Remove(group);
		else
			expandedGroups.Add(group);
	}

	private void ToggleAdvancedFilterValue(string group, string value, bool selected)
	{
		if (!selectedFilters.ContainsKey(group))
			selectedFilters[group] = new HashSet<string>();

		if (selected)
			selectedFilters[group].Add(value);
		else
			selectedFilters[group].Remove(value);

		//StateHasChanged();
	}

	private bool IsAdvancedFilterSelected(string group, string value) =>
		selectedFilters.TryGetValue(group, out var values) && values.Contains(value);


	// Уникальные значения для каждой фильтруемой колонки
	private Dictionary<string, List<string>> GetFilterValues()
	{
		var result = new Dictionary<string, List<string>>();

		foreach (var col in filterableColumns)
		{
			var values = orders.Data.Select(item =>
			{
				var prop = typeof(TListItem).GetProperty(col.PropertyName);
				var value = prop?.GetValue(item);
				return value switch
				{
					bool b => b ? "Да" : "Нет",
					null => "—",
					_ => value.ToString()?.ToLower() ?? "—"
				};
			}).Distinct().OrderBy(v => v).ToList();

			result[col.Title] = values;
		}

		return result;
	}

	// Отфильтрованные элементы
	private IEnumerable<TListItem> FilteredItems
	{
		get
		{
			var filtered = orders.Data;
			var filters = new List<FilterCondition>();

			foreach (var filter in selectedFilters.Where(f => f.Value.Count > 0))
			{
				var col = columnInfos.FirstOrDefault(c => c.Title == filter.Key);
				if (col == null) continue;

				var prop = typeof(TListItem).GetProperty(col.PropertyName);
				if (prop == null) continue;

				filtered = filtered.Where(item =>
				{
					var value = prop.GetValue(item);
					var stringValue = value switch
					{
						bool b => b ? "Да" : "Нет",
						null => "—",
						_ => value.ToString() ?? "—"
					};
					return filter.Value.Contains(stringValue);
				}).ToList();
			}

			return filtered;
		}
	}

	private List<FilterCondition> BuildFilterConditionsForIn()
	{
		var filters = new List<FilterCondition>();

		foreach (var filter in selectedFilters.Where(f => f.Value.Count > 0))
		{
			var col = columnInfos.FirstOrDefault(c => c.Title == filter.Key);
			if (col == null) continue;

			var prop = typeof(TListItem).GetProperty(col.PropertyName);
			if (prop == null) continue;


			// Преобразуем значения
			var convertedValues = filter.Value
				.Select(v => ConvertFilterValue(v, prop.PropertyType))
				//.Where(v => !string.IsNullOrEmpty(v))
				.ToList();

			if (!convertedValues.Any())
				continue;

			var filterCondition = new FilterCondition ();
			
			filterCondition.Field = col.PropertyName;
			filterCondition.Values = convertedValues!;
			filterCondition.Operator = FilterOperators.In;

			// Если фильтр по строке с одним значением — используем Contains
			if (prop.PropertyType == typeof(string) && convertedValues.Count == 1)
			{
				filterCondition.Operator = FilterOperators.Contains;
				filterCondition.Value = convertedValues.First();
				//filterCondition.Values = null; // для Contains используем Value, не Values
			}

			filters.Add(filterCondition);
		}

		return filters;
	}

	/// <summary>
	/// Определяет оператор фильтрации по типу свойства
	/// </summary>
	private string GetOperatorForProperty(PropertyInfo prop)
	{
		var type = prop.PropertyType;

		// Для строк — IN (если несколько значений) или Contains (если одно)
		if (type == typeof(string))
			return FilterOperators.In;

		// Для булевых значений — Equal
		if (type == typeof(bool) || type == typeof(bool?))
			return FilterOperators.Equal;

		// Для чисел и дат — можно использовать Between, но по умолчанию IN
		if (type == typeof(int) || type == typeof(int?) ||
			type == typeof(decimal) || type == typeof(decimal?) ||
			type == typeof(DateTime) || type == typeof(DateTime?))
			return FilterOperators.In;

		// Для остальных — IN
		return FilterOperators.In;
	}

	private void ClearAdvancedFilters()
	{
		selectedFilters.Clear();
		StateHasChanged();
	}

	private object? ConvertFilterValue(string value, Type targetType)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		try
		{
			// Для булевых значений
			if (targetType == typeof(bool))
			{
				if (value == "Да") return true;
				if (value == "Нет") return false;
				return bool.Parse(value);
			}

			// Для чисел
			if (targetType == typeof(int) || targetType == typeof(int?))
				return int.Parse(value);

			if (targetType == typeof(decimal) || targetType == typeof(decimal?))
				return decimal.Parse(value);

			if (targetType == typeof(double) || targetType == typeof(double?))
				return double.Parse(value);

			// Для дат
			if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
				return DateTime.Parse(value);

			// Для GUID
			if (targetType == typeof(Guid) || targetType == typeof(Guid?))
				return Guid.Parse(value);

			// Для строк — возвращаем как есть
			return value;
		}
		catch
		{
			return null;
		}
	}

	public void Dispose()
	{
		try
		{
			if (_panelResizeInitialized)
			{
				_ = JSRuntime.InvokeVoidAsync("panelResize.dispose", "split-panel");
			}
		}
		catch { }

		panelResizeRef?.Dispose();

		EventAggregator.Unsubscribe<OrderUpdatedEvent>(OnOrderUpdated);
	}

	// Загрузка фасетов
	private async Task LoadFacetsAsync()
	{
		var fields = columnInfos
			.Where(c => c.Filterable)
			.Select(c => c.PropertyName)
			.ToList();

		if (!fields.Any()) return;

		var request = new FilterFacetsRequestDto { Fields = fields };
		var response = await ApiService.GetFilterFacetsAsync<TListItem>(Endpoint, request);

		facets = response?.Facets ?? [];
	}

	// Группы фильтров на основе фасетов
	private Dictionary<string, List<FacetValueDto>> advancedFilterGroups
	{
		get
		{
			var result = new Dictionary<string, List<FacetValueDto>>();

			foreach (var col in columnInfos.Where(c => c.Filterable))
			{
				if (facets.TryGetValue(col.PropertyName, out var values))
					result[col.Title] = values;
			}

			return result;
		}
	}
}