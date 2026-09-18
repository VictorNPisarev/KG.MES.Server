using KG.MES.Shared.Events;
using KG.MES.Shared.Interfaces;
using KG.MES.Shared.Models.ViewModels;
using KG.MES.Shared.Services;
using KG.MES.UI.Shared.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace KG.MES.UI.Shared.Components.Widgets;

public partial class OrderTraceWidget : ComponentBase, ISavableWidget
{
	[Parameter] public Guid OrderId { get; set; }
	[Parameter] public bool CanEdit { get; set; }

	[Inject] private ProductionApiService ApiService { get; set; } = null!;
	[Inject] private ISocketService SocketService { get; set; } = null!;
	[Inject] private IEventAggregator EventAggregator { get; set; } = null!;

	private OrderTraceViewModel? orderTrace;
	private OrderTraceViewModel? backupTrace;
	private bool isLoading = true;
	private bool EditMode;
	private Dictionary<string, bool> openDropdowns = [];
	private bool isCompleting;
	private bool isDeparturing;


	protected override async Task OnInitializedAsync()
	{
		SocketService.OnMessage += OnSocketMessage;
		var orderTraceDto = await ApiService.GetOrderTraceAsync(OrderId);

		if(orderTraceDto != null)
		{
			orderTrace = new OrderTraceViewModel(orderTraceDto);
		}

		if (orderTrace?.ProductionOrderId != Guid.Empty)
		{
			await SocketService.SubscribeAsync("order", orderTrace?.ProductionOrderId.ToString());
		}

		CanEdit = CanEdit && !(orderTrace?.Departed ?? false); 

		isLoading = false;
	}

	private void OnSocketMessage(string channel, object data)
	{
		if (channel == "order")
		{
			InvokeAsync(async () =>
			{
				var orderTraceDto = await ApiService.GetOrderTraceAsync(OrderId);

				if (orderTraceDto != null)
				{
					orderTrace = new OrderTraceViewModel(orderTraceDto);
					StateHasChanged();
				}
			});
		}
	}

	private void EnterEditMode()
	{
		backupTrace = new OrderTraceViewModel
		{
			OrderNumber = orderTrace?.OrderNumber ?? "",
			WorkplaceTraces = orderTrace?.WorkplaceTraces.Select(w => new WorkplaceTraceViewModel
			{
				WorkplaceId = w.WorkplaceId,
				WorkplaceName = w.WorkplaceName,
				Status = w.Status
			}).ToList() ?? []
		};
		EditMode = true;
	}

	private void CancelEdit()
	{
		if (backupTrace != null)
		{
			orderTrace = backupTrace;
		}
		EditMode = false;
		openDropdowns.Clear();
	}

	private async Task SaveChanges()
	{
		if (orderTrace == null || orderTrace.ProductionOrderId == Guid.Empty) return;

		foreach (var wp in orderTrace.WorkplaceTraces)
		{
			if (wp.WorkplaceId == Guid.Empty) continue;

			var original = backupTrace?.WorkplaceTraces.FirstOrDefault(b => b.WorkplaceId == wp.WorkplaceId);
			if (original == null || wp.Status != original.Status)
			{
				await ApiService.UpdateOrderTraceAsync(
					orderTrace.ProductionOrderId!,
					wp.WorkplaceId,
					wp.Status
				);
			}
		}

		orderTrace = await LoadOrderTrace();
		EditMode = false;
		openDropdowns.Clear();

		EventAggregator.Publish(new OrderUpdatedEvent { OrderId = OrderId });
	}

	private void ToggleDropdown(string key)
	{
		var wasOpen = openDropdowns.GetValueOrDefault(key);
		openDropdowns.Clear();
		if (!wasOpen) openDropdowns[key] = true;
	}

	private void CloseDropdown(string key)
	{
		openDropdowns.Remove(key);
	}

	private async Task CompleteOrder()
	{
		isCompleting = true;
		StateHasChanged();

		var success = await ApiService.SetOrderCompleteAsync(OrderId);

		if (success)
		{
			orderTrace = await LoadOrderTrace();
			EventAggregator.Publish(
				new OrderUpdatedEvent 
				{
					OrderId = OrderId,
					Source = "trace"
				});
		}

		isCompleting = false;
		StateHasChanged();
	}

	private async Task DepartureOrder()
	{
		isDeparturing = true;
		StateHasChanged();

		var success = await ApiService.SetOrderDepartureAsync(OrderId);

		if (success)
		{
			orderTrace = await LoadOrderTrace();
			EventAggregator.Publish(
				new OrderUpdatedEvent
				{
					OrderId = OrderId,
					Source = "trace"
				});
		}

		isDeparturing = false;
		StateHasChanged();
	}

	private async Task<OrderTraceViewModel?> LoadOrderTrace()
	{
		var orderTraceDto = await ApiService.GetOrderTraceAsync(OrderId);

		return orderTraceDto != null ? new OrderTraceViewModel(orderTraceDto) : null;
	}

	public bool HasUnsavedChanges() => EditMode && orderTrace != null && !orderTrace.Equals(backupTrace);
	public Task SaveAllAsync() => SaveChanges();
}