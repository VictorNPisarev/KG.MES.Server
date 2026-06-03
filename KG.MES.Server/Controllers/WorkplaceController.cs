// KG.MES.Server/Controllers/WorkplaceController.cs
using KG.MES.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api")]
public class WorkplaceController : ControllerBase
{
	private readonly IWorkplaceService _workplaceService;
	private readonly ILogger<WorkplaceController> _logger;

	public WorkplaceController(IWorkplaceService workplaceService, ILogger<WorkplaceController> logger)
	{
		_workplaceService = workplaceService;
		_logger = logger;
	}

	// GET: api/workplaces/active
	[HttpGet("workplaces/active")]
	public async Task<IActionResult> GetActiveWorkplaces()
	{
		var workplaces = await _workplaceService.GetActiveWorkplacesAsync();
		return Ok(workplaces);
	}

	// GET: api/workplaces/all
	[HttpGet("workplaces/all")]
	public async Task<IActionResult> GetAllWorkplaces()
	{
		var workplaces = await _workplaceService.GetAllWorkplacesAsync();
		return Ok(workplaces);
	}

	// GET: api/workplaces/{workplaceId}/stats
	[HttpGet("workplaces/{workplaceId}/stats")]
	public async Task<IActionResult> GetWorkplaceStats(Guid workplaceId)
	{
		var stats = await _workplaceService.GetWorkplaceStatsAsync(workplaceId);
		return Ok(stats);
	}

	// GET: api/workplaces/{workplaceId}/history
	[HttpGet("workplaces/{workplaceId}/history")]
	public async Task<IActionResult> GetWorkplaceHistory(
		Guid workplaceId,
		[FromQuery] DateTime? from,
		[FromQuery] DateTime? to,
		[FromQuery] int limit = 50)
	{
		var history = await _workplaceService.GetWorkplaceHistoryAsync(workplaceId, from, to, limit);
		return Ok(history);
	}

	// GET: api/workplaces/{workplaceId}/blocks
	[HttpGet("workplaces/{workplaceId}/blocks")]
	public async Task<IActionResult> GetWorkplaceBlocks(Guid workplaceId)
	{
		var blocks = await _workplaceService.GetWorkplaceBlocksAsync(workplaceId);
		return Ok(blocks);
	}
}