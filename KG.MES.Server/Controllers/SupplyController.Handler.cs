using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services;
using KG.MES.Shared.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

public partial class SupplyController
{
	private readonly ISupplyService supplyService;

	private readonly IOrderService orderService;

	private readonly ILogger<SupplyController> logger;

	public SupplyController(IOrderService orderService, ISupplyService supplyService, ILogger<SupplyController> logger)
	{
		this.orderService = orderService;
		this.supplyService = supplyService;
		this.logger = logger;
	}

	public async Task<IActionResult> GetOrderSupplyStatusesHandler(Guid orderId)
	{
		var items = await supplyService.GetOrderSupplyItemsAsync(orderId);
		return Ok(items);
	}

	public async Task<IActionResult> UpdateSupplyStatusHandler(
		Guid orderId,
		Guid supplyTypeId,
		UpdateSupplyItemRequest request)
	{
		var result = await supplyService.UpdateSupplyItemAsync(orderId, supplyTypeId, request);
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

		var result = await supplyService.UpdateAllSupplyItemsAsync(orderId, request.Supplies);

		if (!result.Success)
			return BadRequest(result);

		return Ok(result);
	}

	public async Task<IActionResult> GetSupplyConditionsHandler()
	{
		var conditions = await supplyService.GetSupplyConditionsAsync();
		return Ok(conditions);
	}

	public async Task<IActionResult> GetSupplyTypesHandler()
	{
		var types = await supplyService.GetSupplyTypesAsync();
		return Ok(types);
	}

	public async Task<IActionResult> GetAllSupplyStatusesHandler(int page = 1, int limit = 50, string? sortBy = "ready_date",
		string? sortOrder = "asc", string? orderNumber = null, Guid? workplaceId = null, List<Guid>? workplaceIds = null)
	{
		var result = await supplyService.GetAllSupplyItemsAsync(page, limit, sortBy, sortOrder, workplaceIds, orderNumber);
		return Ok(result);
	}

	public async Task<IActionResult> GetFilterFacetsHandler(FilterFacetsRequestDto request)
	{
		if (request?.Fields == null || !request.Fields.Any())
			return BadRequest(new { error = "Fields are required" });

		var result = await orderService.GetFilterFacetsAsync<OrderSupplyDto>(request);
		return Ok(result);
	}

	public async Task<IActionResult> GetSupplyHistoryHandler(Guid supplyItemId)
	{
		var history = await supplyService.GetSupplyHistoryAsync(supplyItemId);
		return Ok(history);
	}
}