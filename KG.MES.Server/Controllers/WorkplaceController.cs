// KG.MES.Server/Controllers/WorkplaceController.cs
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
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
		if (to.HasValue)
		{
			to = to.Value.Date.AddDays(1).AddTicks(-1);
		}

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

	// GET: api/workplaces/{id}
	[HttpGet("workplaces/{id}")]
	public async Task<IActionResult> GetWorkplaceById(Guid id)
	{
		// ✅ Правильно: вызываем метод сервиса
		var workplace = await _workplaceService.GetWorkplaceByIdAsync(id);

		if (workplace == null)
			return NotFound(new { error = "Workplace not found" });

		return Ok(workplace);
	}

	// GET: api/workplaces?type=active|all
	[HttpGet("workplaces")]
	public async Task<IActionResult> GetWorkplaces([FromQuery] string? type = "all")
	{
		if (type?.ToLower() == "active")
		{
			// ✅ Вызываем метод сервиса напрямую, а не другой метод контроллера
			var activeWorkplaces = await _workplaceService.GetActiveWorkplacesAsync();
			return Ok(activeWorkplaces);
		}

		// ✅ По умолчанию возвращаем все
		var allWorkplaces = await _workplaceService.GetAllWorkplacesAsync();
		return Ok(allWorkplaces);
	}
}