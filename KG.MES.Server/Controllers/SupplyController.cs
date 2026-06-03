// KG.MES.Server/Controllers/SupplyController.cs
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api")]
public class SupplyController : ControllerBase
{
	private readonly ISupplyService _supplyService;
	private readonly ILogger<SupplyController> _logger;

	public SupplyController(ISupplyService supplyService, ILogger<SupplyController> logger)
	{
		_supplyService = supplyService;
		_logger = logger;
	}

	// GET: api/orders/{orderId}/supplies
	[HttpGet("orders/{orderId}/supplies")]
	public async Task<IActionResult> GetOrderSupplyStatuses(Guid orderId)
	{
		var items = await _supplyService.GetOrderSupplyItemsAsync(orderId);
		return Ok(items);
	}

	// PUT: api/orders/{orderId}/supply/{supplyTypeId}
	[HttpPut("orders/{orderId}/supply/{supplyTypeId}")]
	public async Task<IActionResult> UpdateSupplyStatus(
		Guid orderId,
		Guid supplyTypeId,
		[FromBody] UpdateSupplyItemRequest request)
	{
		var result = await _supplyService.UpdateSupplyItemAsync(orderId, supplyTypeId, request);
		if (!result.Success)
			return BadRequest(result);
		return Ok(result);
	}

	// PUT: api/orders/{orderId}/supplies
	[HttpPut("orders/{orderId}/supplies")]
	public async Task<IActionResult> UpdateAllSupplyStatuses(
		Guid orderId,
		[FromBody] List<UpdateSupplyItemRequest> updates)
	{
		var result = await _supplyService.UpdateAllSupplyItemsAsync(orderId, updates);
		return Ok(result);
	}

	// GET: api/supplies/conditions
	[HttpGet("supplies/conditions")]
	public async Task<IActionResult> GetSupplyConditions()
	{
		var conditions = await _supplyService.GetSupplyConditionsAsync();
		return Ok(conditions);
	}

	// GET: api/supplies/types
	[HttpGet("supplies/types")]
	public async Task<IActionResult> GetSupplyTypes()
	{
		var types = await _supplyService.GetSupplyTypesAsync();
		return Ok(types);
	}

	// GET: api/supplies
	[HttpGet("supplies")]
	public async Task<IActionResult> GetAllSupplyStatuses(
		[FromQuery] int page = 1,
		[FromQuery] int limit = 100,
		[FromQuery] string? orderNumber = null)
	{
		var result = await _supplyService.GetAllSupplyItemsAsync(page, limit, orderNumber);
		return Ok(result);
	}
}