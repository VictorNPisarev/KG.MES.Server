using KG.MES.Shared.Constants;
using KG.MES.Shared.Models.Config;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;
using Mapster;
using KG.MES.Shared.Models.ViewModels;

namespace KG.MES.UI.Shared.Components;

public partial class WorkplaceStats
{
	[Inject] private ProductionApiService ApiService {get; set;} = null!;
	[Inject] private OrderViewSettings AppSettings { get; set; } = null!;

	private List<WorkplaceDto> workplaces = [];
	private WorkplaceStatsDto? stats;
	private List<BlockedOrderDto> blocks = [];
	private List<WorkplaceHistoryViewModel> workplaceHistory = [];
	private Guid? selectedWorkplaceId;
	private WorkplaceDto? selectedWorkplace;
	private DateTime dateFrom = DateTime.Now.AddDays(-7);
	private DateTime dateTo = DateTime.Now;
	private bool showDateFilter;
	private string activeTab = "history";
	private List<WorkplaceOrderViewModel> workplaceOrders = [];
	private string orderFilter = "all";
	private string historyFilter = "all";

	private DateTime orderDateFrom = DateTime.Now.AddDays(-7);
	private DateTime orderDateTo = DateTime.Now;
	private bool showOrderDateFilter;

	protected override async Task OnInitializedAsync()
	{
		Console.WriteLine("WorkplaceStats OnInitializedAsync");
		workplaces = await ApiService.GetWorkplacesAsync();//("active");
	}

	private async Task SelectWorkplace(WorkplaceDto wp)
	{
		selectedWorkplace = wp;
		activeTab = selectedWorkplace.IsWorkplace ? activeTab : "history";
		activeTab = selectedWorkplace.Code == WorkplaceCodes.None ? "orders" : activeTab;
		await SelectWorkplace(wp.Id);

	}

	private async Task SelectWorkplace(Guid id)
	{
		selectedWorkplaceId = id;
		stats = await ApiService.GetWorkplaceStatsAsync(id);
		blocks = await ApiService.GetWorkplaceBlocksAsync(id);
		
		var historyDto = await ApiService.GetWorkplaceHistoryAsync(id, dateFrom, dateTo, 1000);
		workplaceHistory = new List<WorkplaceHistoryViewModel>().CreateWithTransferIndicators(historyDto, dateFrom, dateTo);
		
		var workplaceOrdersDto = await ApiService.GetActiveAndPendingOrdersAsync(id);
		workplaceOrders = workplaceOrdersDto.Select(wp => wp.Adapt<WorkplaceOrderViewModel>()).ToList();


		StateHasChanged();
	}

	private void ClearSelection()
	{
		selectedWorkplaceId = null;
		stats = null;
		blocks.Clear();
		workplaceHistory.Clear();
		workplaceOrders.Clear();
		StateHasChanged();
	}

	private async Task ApplyFilter()
	{
		showDateFilter = false;

		if (selectedWorkplaceId == null) return;
		
		var historyDto = await ApiService.GetWorkplaceHistoryAsync((Guid)selectedWorkplaceId, dateFrom, dateTo, 1000);

		workplaceHistory = new List<WorkplaceHistoryViewModel>().CreateWithTransferIndicators(historyDto, dateFrom, dateTo);

		StateHasChanged();
	}

	private OrderViewSettings GetWorkplaceOrderSettings() => new()
	{
		ListEndpoint = $"orders/workplaces/{selectedWorkplaceId}/in-work",
		CardEndpoint = AppSettings.CardEndpoint,
		Title = "Заказы участка",
		ShowActions = false,
		CanEdit = false,
		CanExport = false,
		CanDelete = false,
		ShowTrace = false,
		ShowSupply = false,
		EditSupply = false
	};

	//private List<WorkplaceHistoryDto> filteredHistory =>
	//	historyFilter == "all"
	//		? history
	//		: history.Where(h => h.OperationType == historyFilter).ToList();
	private List<WorkplaceHistoryViewModel> filteredHistory =>
		historyFilter == "all"
			? workplaceHistory
			: workplaceHistory.Where(h => h.OperationType == historyFilter).ToList();

	//private List<WorkplaceHistoryDto> distinctHistory => filteredHistory
	//	.GroupBy(h => h.OrderNumber)
	//	.Select(g => g.First())
	//	.ToList();
	private List<WorkplaceHistoryViewModel> distinctHistory => filteredHistory
		.GroupBy(h => h.OrderNumber)
		.Select(g => g.First())
		.ToList();

	private int TotalHistoryWindows => distinctHistory.Sum(h => h.WindowCount);
	private decimal TotalHistoryWindowArea => distinctHistory.Sum(h => h.WindowArea ?? 0);
	private int TotalHistoryPlates => distinctHistory.Sum(h => h.PlateCount);
	private decimal TotalHistoryPlateArea => distinctHistory.Sum(h => h.PlateArea ?? 0);

	private List<WorkplaceOrderViewModel> filteredOrders => orderFilter == "all"
		? workplaceOrders
		: workplaceOrders
			.Where(o => o.Status.ToLower() == orderFilter)
			.OrderBy(w => w.ReadyDate)
			.ToList();

	private List<WorkplaceOrderViewModel> distinctOrders => filteredOrders
		.GroupBy(h => h.OrderNumber)
		.Select(g => g.First())
		.ToList();

	// Итоги по заказам
	private int TotalWindows => distinctOrders.Sum(o => o.WindowCount);
	private decimal TotalWindowArea => distinctOrders.Sum(o => o.WindowArea ?? 0);
	private int TotalPlates => distinctOrders.Sum(o => o.PlateCount);
	private decimal TotalPlateArea => distinctOrders.Sum(o => o.PlateArea ?? 0);
}