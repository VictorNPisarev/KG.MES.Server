using System.Text.Json;
using KG.MES.Shared.Events;
using KG.MES.Shared.Interfaces;
using KG.MES.Shared.Models.Config;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Enums;
using KG.MES.Shared.Models.ViewModels;
using KG.MES.Shared.Services;
using KG.MES.UI.Shared.Components.Widgets;
using KG.MES.UI.Shared.Enums;
using KG.MES.UI.Shared.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace KG.MES.UI.Shared.Components;

public partial class OrderDashboard<TOrder> : ComponentBase
{
	[Parameter] public Guid OrderId { get; set; }
	[Parameter] public OrderViewSettings Settings { get; set; } = new();
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public DashboardViewMode ViewMode { get; set; } = DashboardViewMode.Grid;
	[Parameter] public Func<Guid, Task<TOrder>>? LoadItem { get; set; }

	[Inject] private ProductionApiService ApiService { get; set; } = null!;
	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;
	[Inject] private IEventAggregator EventAggregator { get; set; } = null!;

	private TOrder? order;

	private DashboardSettings dashboardSettings = new();
	private bool isLoading = true;
	private bool showWidgetSettings;

	private OrderSuppliesWidget? suppliesWidget;
	private OrderTraceWidget? traceWidget;
	private OrderFieldsWidget<TOrder>? fieldsWidget;
	//private OrderFieldsWidget<OrderDto>? fieldsWidget;
	private OrderCommentsWidget? commentsWidget;

	private List<WidgetSettings> visibleWidgets => dashboardSettings.Widgets
		.Where(w => w.Visible)
		.OrderBy(w => w.Order)
		.ToList();

	private string dashboardKey => $"dashboard_{typeof(TOrder).Name}";

	protected override async Task OnInitializedAsync()
	{
		EventAggregator.Subscribe<OrderUpdatedEvent>(OnOrderUpdated);
		await LoadDashboard();
		await LoadOrder();
	}

	private async Task LoadDashboard()
	{
		try
		{
			var json = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"dashboard_{dashboardKey}");
			var defaults = GetDefaultDashboard();

			if (!string.IsNullOrEmpty(json))
			{
				var saved = JsonSerializer.Deserialize<DashboardSettings>(json,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				if (saved?.Widgets != null)
				{
					// Удаляем те, которых нет в дефолтных
					saved.Widgets.RemoveAll(w => !defaults.Widgets.Any(d => d.WidgetId == w.WidgetId));

					// Добавляем недостающие из дефолтных
					foreach (var def in defaults.Widgets)
					{
						if (!saved.Widgets.Any(w => w.WidgetId == def.WidgetId))
						{
							saved.Widgets.Add(def);
						}
					}

					dashboardSettings = saved;
					return;
				}
			}

			dashboardSettings = defaults;
		}
		catch
		{
			dashboardSettings = GetDefaultDashboard();
		}
	}

	private DashboardSettings GetDefaultDashboard()
	{
		var widgets = new List<WidgetSettings>
		{
			new() { WidgetId = WidgetType.Main, Title = "Основные данные", Order = 0 },
			new() { WidgetId = WidgetType.Comments, Title = "Примечания", Order = 1 }
		};

		if (Settings.ShowTrace)
			widgets.Add(new() { WidgetId = WidgetType.Trace, Title = "История прохождения", Order = widgets.Count });

		if (Settings.ShowSupply)
			widgets.Add(new() { WidgetId = WidgetType.Supplies, Title = "Снабжение", Order = widgets.Count });

		return new DashboardSettings { Widgets = widgets };
	}

	private async Task SaveDashboard()
	{
		var json = JsonSerializer.Serialize(dashboardSettings);
		await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"dashboard_{dashboardKey}", json);
		StateHasChanged();
	}

	private void ToggleWidgetSettings() => showWidgetSettings = !showWidgetSettings;

	private async Task ToggleWidget(WidgetType widgetId, bool visible)
	{
		var widget = dashboardSettings.Widgets.FirstOrDefault(w => w.WidgetId == widgetId);
		if (widget != null)
		{
			widget.Visible = visible;
			await SaveDashboard();
		}
	}

	private async Task MoveWidget(WidgetType widgetId, int direction)
	{
		var widget = dashboardSettings.Widgets.FirstOrDefault(w => w.WidgetId == widgetId);
		if (widget == null) return;

		var ordered = dashboardSettings.Widgets.OrderBy(w => w.Order).ToList();
		var idx = ordered.IndexOf(widget);
		var newIdx = idx + direction;
		if (newIdx < 0 || newIdx >= ordered.Count) return;

		(ordered[idx].Order, ordered[newIdx].Order) = (ordered[newIdx].Order, ordered[idx].Order);
		await SaveDashboard();
	}

	private async Task LoadOrder()
	{
		isLoading = true;
		StateHasChanged();
		try
		{
			//order = await ApiService.GetOrderByIdAsync<TOrder>(Settings.CardEndpoint, OrderId);
			//var orderDto = await ApiService.GetOrderByIdAsync<OrderDto>(Settings.CardEndpoint, OrderId);
			if (LoadItem != null)
				order = await LoadItem(OrderId);
		}
		finally
		{
			isLoading = false;
			StateHasChanged();
		}
	}

	private WidgetSettings? draggedWidget;
	private WidgetSettings? dragOverWidget;

	private DotNetObjectReference<OrderDashboard<TOrder>>? dotnetRef;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			dotnetRef = DotNetObjectReference.Create(this);
			await JSRuntime.InvokeVoidAsync("dashboardDragDrop.init", dotnetRef);
		}
	}

	[JSInvokable]
	public async Task OnWidgetDrop(string draggedId, string targetId)
	{
		Console.WriteLine($"=== OnWidgetDrop ===");
		Console.WriteLine($"draggedId: {draggedId}, targetId: {targetId}");

		var dragged = dashboardSettings.Widgets.FirstOrDefault(w => w.WidgetId.ToString() == draggedId);
		var target = dashboardSettings.Widgets.FirstOrDefault(w => w.WidgetId.ToString() == targetId);

		if (dragged == null) { Console.WriteLine("dragged is null"); return; }
		if (target == null) { Console.WriteLine("target is null"); return; }
		if (dragged == target) { Console.WriteLine("dragged == target"); return; }

		var ordered = dashboardSettings.Widgets.OrderBy(w => w.Order).ToList();

		Console.WriteLine($"Before:");
		for (int i = 0; i < ordered.Count; i++)
			Console.WriteLine($"  [{i}] {ordered[i].Title} (Order={ordered[i].Order})");

		var draggedIdx = ordered.IndexOf(dragged);
		var targetIdx = ordered.IndexOf(target);

		Console.WriteLine($"draggedIdx={draggedIdx}, targetIdx={targetIdx}");

		// Вынимаем
		ordered.RemoveAt(draggedIdx);

		// Вставляем на позицию цели
		ordered.Insert(targetIdx, dragged);

		// Перенумеровываем
		for (int i = 0; i < ordered.Count; i++)
			ordered[i].Order = i;

		Console.WriteLine($"After:");
		for (int i = 0; i < ordered.Count; i++)
			Console.WriteLine($"  [{i}] {ordered[i].Title} (Order={ordered[i].Order})");

		await SaveDashboard();
		StateHasChanged();
	}
	public void Dispose()
	{
		EventAggregator.Unsubscribe<OrderUpdatedEvent>(OnOrderUpdated);
		dotnetRef?.Dispose();
	}
	private string GetWidgetClass(WidgetSettings widget)
	{
		var classes = new List<string>();

		if (widget.Column == 2) classes.Add("widget-span-2");
		if (widget == dragOverWidget) classes.Add("drag-over");

		return string.Join(" ", classes);
	}

	private string? GetNotes(TOrder order) =>
		order?.GetType().GetProperty("MasterNotes")?.GetValue(order)?.ToString() ?? "—";

	private string activeTab = "main";

	private List<WidgetSettings> gridWidgets => dashboardSettings.Widgets
	.Where(w => w.Visible && !w.IsTabbed)
	.OrderBy(w => w.Order)
	.ToList();

	private List<WidgetSettings> tabbedWidgets => dashboardSettings.Widgets
	.Where(w => w.Visible && w.IsTabbed)
	.OrderBy(w => w.Order)
	.ToList();

	private void ActivateMainTab() => ActivateTab("main");
	private void ActivateTab(string tabId) { activeTab = tabId; StateHasChanged(); }

	private void OnTabDragOver(DragEventArgs e) { }

	[JSInvokable]
	public async Task OnDropToTabs(string draggedId)
	{
		Console.WriteLine($"OnDropToTabs: {draggedId}");

		var widget = dashboardSettings.Widgets.FirstOrDefault(w => w.WidgetId.ToString() == draggedId);
		if (widget != null && !widget.IsTabbed)
		{
			widget.IsTabbed = true;
			activeTab = draggedId;
			await SaveDashboard();
			StateHasChanged();
		}
	}

	[JSInvokable]
	public async Task OnDropToMain(string draggedId)
	{
		Console.WriteLine($"OnDropToMain: {draggedId}");

		var widget = dashboardSettings.Widgets.FirstOrDefault(w => w.WidgetId.ToString() == draggedId);
		if (widget != null && widget.IsTabbed)
		{
			widget.IsTabbed = false;
			activeTab = "main";
			await SaveDashboard();
			StateHasChanged();
		}
	}

	[JSInvokable]
	public async Task OnDropTabToWidget(string draggedId, string targetId)
	{
		Console.WriteLine($"OnDropTabToWidget: {draggedId} -> {targetId}");

		var dragged = dashboardSettings.Widgets.FirstOrDefault(w => w.WidgetId.ToString() == draggedId);
		var target = dashboardSettings.Widgets.FirstOrDefault(w => w.WidgetId.ToString() == targetId);

		if (dragged == null || target == null) return;

		dragged.IsTabbed = false;
		activeTab = "main";

		await SaveDashboard();
		StateHasChanged();
	}

	[JSInvokable]
	public async Task OnWidgetResize(string widgetId, string width)
	{
		var widget = dashboardSettings.Widgets.FirstOrDefault(w => w.WidgetId.ToString() == widgetId);
		if (widget != null)
		{
			widget.CustomWidth = width;
			await SaveDashboard();
			StateHasChanged();
		}
	}

	public async Task CloseOrder()
	{
		Console.WriteLine("=== CloseOrder called ===");

		var widgets = new ISavableWidget[] { fieldsWidget!, traceWidget!, suppliesWidget! };
		var hasChanges = widgets.Any(w => w?.HasUnsavedChanges() == true);

		Console.WriteLine($"HasUnsavedChanges: {hasChanges}");

		if (hasChanges)
		{
			var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Есть несохранённые изменения. Сохранить?");
			Console.WriteLine($"Confirmed: {confirmed}");
			if (confirmed)
			{
				foreach (var w in widgets)
					if (w != null)
						await w.SaveAllAsync();
			}
		}

		await OnClose.InvokeAsync();
	}

	private async void OnOrderUpdated(OrderUpdatedEvent e)
	{
		if (e.OrderId == OrderId)
		{
			await LoadOrder();
		}
	}

	private RenderFragment RenderWidgetContent(WidgetSettings widget) => widget.WidgetId switch
	{
		WidgetType.Main => builder =>
		{
			builder.OpenComponent<OrderFieldsWidget<TOrder>>(0);
			builder.AddComponentParameter(1, "Order", order);
			builder.AddComponentParameter(2, "Settings", Settings);
			builder.CloseComponent();
		}
		,
		WidgetType.Trace => builder =>
		{
			builder.OpenComponent<OrderTraceWidget>(0);
			builder.AddComponentParameter(1, "OrderId", OrderId);
			builder.AddComponentParameter(2, "CanEdit", Settings.EditTrace);
			builder.CloseComponent();
		}
		,
		WidgetType.Supplies => builder =>
		{
			builder.OpenComponent<OrderSuppliesWidget>(0);
			builder.AddComponentParameter(1, "OrderId", OrderId);
			builder.AddComponentParameter(2, "CanEdit", Settings.EditSupply);
			builder.CloseComponent();
		}
		,
		WidgetType.Comments => builder =>
		{
			builder.OpenComponent<OrderCommentsWidget>(0);
			builder.AddComponentParameter(1, "OrderId", OrderId);
			builder.CloseComponent();
		}
		,
		_ => builder => { }
	};
}