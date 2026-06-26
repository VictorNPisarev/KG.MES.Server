// KG.MES.Server/Controllers/WorkplaceController.cs
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api")]
public partial class WorkplaceController : ControllerBase
{
	// GET: api/workplaces/active
	[HttpGet("workplaces/active")]
	public Task<IActionResult> GetActiveWorkplaces() => GetActiveWorkplacesHandler();

	// GET: api/workplaces/all
	[HttpGet("workplaces/all")]
	public Task<IActionResult> GetAllWorkplaces() => GetAllWorkplacesHandler();

	// GET: api/workplaces/{workplaceId}/stats
	[HttpGet("workplaces/{workplaceId}/stats")]
	public Task<IActionResult> GetWorkplaceStats(Guid workplaceId) => GetWorkplaceStatsHandler(workplaceId);

	// GET: api/workplaces/{workplaceId}/history
	[HttpGet("workplaces/{workplaceId}/history")]
	public Task<IActionResult> GetWorkplaceHistory(
		Guid workplaceId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int limit = 50)
	=> GetWorkplaceHistoryHandler(workplaceId, from, to, limit);

	// GET: api/workplaces/{workplaceId}/blocks
	[HttpGet("workplaces/{workplaceId}/blocks")]
	public Task<IActionResult> GetWorkplaceBlocks(Guid workplaceId) => GetWorkplaceBlocksHandler(workplaceId);

	// GET: api/workplaces/{id}
	[HttpGet("workplaces/{id}")]
	public Task<IActionResult> GetWorkplaceById(Guid id) => GetWorkplaceByIdHandler(id);

	// GET: api/workplaces?type=active|all
	[HttpGet("workplaces")]
	public Task<IActionResult> GetWorkplaces([FromQuery] string? type = "all") => GetWorkplacesHandler(type);
}