// KG.MES.Server/Controllers/SupplyController.cs
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api")]
public partial class SupplyController : ControllerBase
{
	// GET: api/orders/{orderId}/supplies
	[HttpGet("orders/{orderId}/supplies")]
	public Task<IActionResult> GetOrderSupplyStatuses(Guid orderId) => GetOrderSupplyStatusesHandler(orderId);

	// PUT: api/orders/{orderId}/supply/{supplyTypeId}
	[HttpPut("orders/{orderId}/supply/{supplyTypeId}")]
	public Task<IActionResult> UpdateSupplyStatus(Guid orderId, Guid supplyTypeId, [FromBody] UpdateSupplyItemRequest request)
		=> UpdateSupplyStatusHandler(orderId, supplyTypeId, request);

	// PUT: api/orders/{orderId}/supplies
	[HttpPut("orders/{orderId}/supplies")]
	public Task<IActionResult> UpdateAllSupplyStatuses(Guid orderId, [FromBody] UpdateOrderSupplyItemsRequestDto request)
		=> UpdateAllSupplyStatusesHandler(orderId, request);
	
	// GET: api/supplies/conditions
	[HttpGet("supplies/conditions")]
	public Task<IActionResult> GetSupplyConditions() => GetSupplyConditionsHandler();

	// GET: api/supplies/types
	[HttpGet("supplies/types")]
	public Task<IActionResult> GetSupplyTypes() => GetSupplyTypesHandler();

	// GET: api/supplies
	[HttpGet("supplies")]
	public Task<IActionResult> GetAllSupplyStatuses([FromQuery] int page = 1, [FromQuery] int limit = 50, [FromQuery] string? sortBy = "ready_date",
			[FromQuery] string? sortOrder = "asc", [FromQuery] Guid? workplaceId = null, [FromQuery] string? orderNumber = null)
		=> GetAllSupplyStatusesHandler(page, limit, sortBy, sortOrder, workplaceId, orderNumber);
}