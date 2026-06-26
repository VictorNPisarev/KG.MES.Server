using KG.MES.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

public partial class WorkplaceController
{
	private readonly IWorkplaceService _workplaceService;
	private readonly ILogger<WorkplaceController> _logger;

	public WorkplaceController(IWorkplaceService workplaceService, ILogger<WorkplaceController> logger)
	{
		_workplaceService = workplaceService;
		_logger = logger;
	}

	public async Task<IActionResult> GetActiveWorkplacesHandler()
	{
		var workplaces = await _workplaceService.GetActiveWorkplacesAsync();
		return Ok(workplaces);
	}

	public async Task<IActionResult> GetAllWorkplacesHandler()
	{
		var workplaces = await _workplaceService.GetAllWorkplacesAsync();
		return Ok(workplaces);
	}

	public async Task<IActionResult> GetWorkplaceStatsHandler(Guid workplaceId)
	{
		var stats = await _workplaceService.GetWorkplaceStatsAsync(workplaceId);
		return Ok(stats);
	}

	public async Task<IActionResult> GetWorkplaceHistoryHandler(
		Guid workplaceId, DateTime? from, DateTime? to, int limit)
	{
		if (to.HasValue)
		{
			to = to.Value.Date.AddDays(1).AddTicks(-1);
		}

		var history = await _workplaceService.GetWorkplaceHistoryAsync(workplaceId, from, to, limit);
		return Ok(history);
	}

	public async Task<IActionResult> GetWorkplaceBlocksHandler(Guid workplaceId)
	{
		var blocks = await _workplaceService.GetWorkplaceBlocksAsync(workplaceId);
		return Ok(blocks);
	}

	public async Task<IActionResult> GetWorkplaceByIdHandler(Guid id)
	{
		var workplace = await _workplaceService.GetWorkplaceByIdAsync(id);

		if (workplace == null)
			return NotFound(new { error = "Workplace not found" });

		return Ok(workplace);
	}

	public async Task<IActionResult> GetWorkplacesHandler(string? type)
	{
		if (type?.ToLower() == "active")
		{
			var activeWorkplaces = await _workplaceService.GetActiveWorkplacesAsync();
			return Ok(activeWorkplaces);
		}

		var allWorkplaces = await _workplaceService.GetAllWorkplacesAsync();
		return Ok(allWorkplaces);
	}
}