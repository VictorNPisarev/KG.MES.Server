using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services;
using KG.MES.UI.Shared.Interfaces;
using Microsoft.AspNetCore.Components;

namespace KG.MES.UI.Shared.Components.Widgets;

public partial class WorkplaceStatsWidget : ComponentBase, ISavableWidget
{
	private List<WorkplaceDto> workplaces = new();
	private WorkplaceStatsDto? stats;
	private List<BlockedOrderDto> blocks = new();
	private List<WorkplaceHistoryDto> history = new();
	private Guid? selectedWorkplaceId;

	protected override async Task OnInitializedAsync()
	{
		workplaces = await ApiService.GetWorkplacesAsync("active");
	}

	private async Task LoadStats()
	{
		if (selectedWorkplaceId == null) return;

		Guid id = (Guid)selectedWorkplaceId;
		stats = await ApiService.GetWorkplaceStatsAsync(id);
		blocks = await ApiService.GetWorkplaceBlocksAsync(id);
		history = await ApiService.GetWorkplaceHistoryAsync(
		id,
		DateTime.Now.AddDays(-7),
		DateTime.Now,
		1000);
	}

	public bool HasUnsavedChanges() => false;
	public Task SaveAllAsync() => Task.CompletedTask;
}