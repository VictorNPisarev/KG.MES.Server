using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

public partial class SupplyController
{
	private readonly ISupplyService _supplyService;
	private readonly ILogger<SupplyController> _logger;

	public SupplyController(ISupplyService supplyService, ILogger<SupplyController> logger)
	{
		_supplyService = supplyService;
		_logger = logger;
	}

	public async Task<IActionResult> GetOrderSupplyStatusesHandler(Guid orderId)
	{
		var items = await _supplyService.GetOrderSupplyItemsAsync(orderId);
		return Ok(items);
	}

	public async Task<IActionResult> UpdateSupplyStatusHandler(
		Guid orderId,
		Guid supplyTypeId,
		UpdateSupplyItemRequest request)
	{
		var result = await _supplyService.UpdateSupplyItemAsync(orderId, supplyTypeId, request);
		if (!result.Success)
			return BadRequest(result);
		return Ok(result);
	}

	public async Task<IActionResult> UpdateAllSupplyStatusesHandler(
		Guid orderId,
		UpdateOrderSupplyItemsRequestDto request)
	{
		if (request?.Supplies == null || request.Supplies.Count == 0)
		{
			return BadRequest(new { error = "supplies array is required" });
		}

		var result = await _supplyService.UpdateAllSupplyItemsAsync(orderId, request.Supplies);

		if (!result.Success)
			return BadRequest(result);

		return Ok(result);
	}

	public async Task<IActionResult> GetSupplyConditionsHandler()
	{
		var conditions = await _supplyService.GetSupplyConditionsAsync();
		return Ok(conditions);
	}

	public async Task<IActionResult> GetSupplyTypesHandler()
	{
		var types = await _supplyService.GetSupplyTypesAsync();
		return Ok(types);
	}

	public async Task<IActionResult> GetAllSupplyStatusesHandler(int page = 1, int limit = 50, string? sortBy = "ready_date",
		string? sortOrder = "asc", Guid? workplaceId = null, string? orderNumber = null)
	{
		var result = await _supplyService.GetAllSupplyItemsAsync(page, limit, sortBy, sortOrder, workplaceId, orderNumber);
		return Ok(result);
	}
}