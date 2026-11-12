using KG.MES.Shared.Constants;
using KG.MES.Shared.Events;
using KG.MES.Shared.Helpers;
using KG.MES.Shared.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.ViewModels;
using KG.MES.Shared.Services;
using KG.MES.UI.Shared.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace KG.MES.UI.Shared.Components.Widgets;

public partial class OrderTraceWidget : ComponentBase, ISavableWidget
{
	[Parameter] public Guid OrderId { get; set; }
	[Parameter] public bool CanEdit { get; set; }
	[Parameter] public bool ShowLogs { get; set; } = false;

	[Inject] private ProductionApiService ApiService { get; set; } = null!;
	[Inject] private ISocketService SocketService { get; set; } = null!;
	[Inject] private IEventAggregator EventAggregator { get; set; } = null!;
	[Inject] private UserSessionService Session { get; set; } = null!;

	private OrderTraceViewModel? orderTrace;
	private OrderTraceViewModel? backupTrace;
	private bool isLoading = true;
	private bool EditMode;
	private Dictionary<string, bool> openDropdowns = [];
	private bool isCompleting;
	private bool isDeparturing;
	private List<OrderOperationLogDto> operationLogs = [];
	private List<WorkplaceOperationSummary> pivotLogs = [];
	private bool showLogs;
	private bool isLoadingLogs;
	private Guid? logsFilterWorkplaceId;
	private string? logsFilterWorkplaceName;
	private ElementReference logsOverlayRef;

	private IEnumerable<OrderOperationLogDto> filteredLogs =>
	logsFilterWorkplaceId.HasValue
		? operationLogs
			.Where(l => l.WorkplaceId == logsFilterWorkplaceId.Value)
			.OrderByDescending(l => l.OperationTime)
		: operationLogs
			.OrderByDescending(l => l.OperationTime);


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

	private void OnSocketMessage(string channel, object? data)
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

	private async Task OpenAllLogs()
	{
		logsFilterWorkplaceId = null;
		logsFilterWorkplaceName = null;
		await OpenLogsInternal();
	}

	private async Task OpenWorkplaceLogs(WorkplaceTraceViewModel trace)
	{
		logsFilterWorkplaceId = trace.WorkplaceId;
		logsFilterWorkplaceName = trace.WorkplaceName;
		await OpenLogsInternal();
	}

	private async Task OpenLogsInternal()
	{
		showLogs = true;
		isLoadingLogs = true;
		operationLogs = [];
		pivotLogs = [];
		StateHasChanged();

		try
		{
			var identifier = orderTrace?.OrderNumber;
			if (string.IsNullOrEmpty(identifier))
				identifier = OrderId.ToString();

			operationLogs = await ApiService.GetOrderOperationLogsAsync(identifier);
			pivotLogs = BuildPivot(operationLogs);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Failed to load operation logs: {ex.Message}");
		}
		finally
		{
			isLoadingLogs = false;
			StateHasChanged();
		}

		await Task.Yield();
		try { await logsOverlayRef.FocusAsync(); } catch { }
	}

	private void CloseLogs()
	{
		showLogs = false;
		logsFilterWorkplaceId = null;
		logsFilterWorkplaceName = null;
		operationLogs = [];
		pivotLogs = [];
	}

	private void OnLogsKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Escape")
			CloseLogs();
	}

	/// <summary>
	/// Схлопывает логи в pivot: одна строка = один участок.
	/// Берётся ПЕРВЫЙ START и ПОСЛЕДНИЙ COMPLETE на участке (вариант A).
	/// </summary>
	private static List<WorkplaceOperationSummary> BuildPivot(List<OrderOperationLogDto> logs)
	{
		var result = new List<WorkplaceOperationSummary>();

		foreach (var group in logs.GroupBy(l => l.WorkplaceId))
		{
			var startLogs = group
				.Where(l => l.OperationType == OrderStatus.OperationType.Start)
				.OrderBy(l => l.OperationTime)
				.ToList();

			var completeLogs = group
				.Where(l => l.OperationType == OrderStatus.OperationType.Complete)
				.OrderBy(l => l.OperationTime)
				.ToList();

			if (startLogs.Count == 0 && completeLogs.Count == 0)
				continue;

			var firstStart = startLogs.FirstOrDefault();
			var lastComplete = completeLogs.LastOrDefault();
			var any = group.First();

			result.Add(new WorkplaceOperationSummary
			{
				WorkplaceId = group.Key,
				WorkplaceName = any.WorkplaceName,
				WorkplaceCode = any.WorkplaceCode,
				StartedAt = firstStart?.OperationTime,
				CompletedAt = lastComplete?.OperationTime,
				StartedByUserName = firstStart?.UserName,
				CompletedByUserName = lastComplete?.UserName,
				StartedNotes = firstStart?.Notes,
				CompletedNotes = lastComplete?.Notes
			});
		}

		return result
			.OrderBy(r => r.StartedAt ?? r.CompletedAt ?? DateTime.MaxValue)
			.ThenBy(r => r.WorkplaceName)
			.ToList();
	}

	/// <summary> Класс бейджа для участка — тот же, что в списке заказов. </summary>
	private static string GetWorkplaceBadgeClass(string? name)
		=> BadgeHelper.GetBadgeClass(name, "workplaces");

	public class WorkplaceOperationSummary
	{
		public Guid WorkplaceId { get; set; }
		public string WorkplaceName { get; set; } = string.Empty;
		public string? WorkplaceCode { get; set; }
		public DateTime? StartedAt { get; set; }
		public DateTime? CompletedAt { get; set; }
		public string? StartedByUserName { get; set; }
		public string? CompletedByUserName { get; set; }
		public string? StartedNotes { get; set; }
		public string? CompletedNotes { get; set; }
		public bool InProgress => StartedAt.HasValue && !CompletedAt.HasValue;
	}
}