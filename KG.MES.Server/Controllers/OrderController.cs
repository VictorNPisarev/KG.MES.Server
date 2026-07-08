
using KG.MES.Server.Models.Dto;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api")]
public partial class OrderController : ControllerBase
{
	// GET: api/orders
	[HttpGet("orders")]
	public Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int limit = 50, [FromQuery] string? sortBy = "ready_date",
			[FromQuery] string? sortOrder = "asc", [FromQuery] string? orderNumber = null, 
			[FromQuery] Guid? workplaceId = null, [FromQuery] List<Guid>? workplaceIds = null)
		=> GetOrdersHandler(page, limit, sortBy, sortOrder, orderNumber, workplaceId, workplaceIds);

	// GET: api/orders/pending?workplaceId=...
	[HttpGet("orders/pending")]
	public Task<IActionResult> GetPendingOrdersCompatible([FromQuery] Guid workplaceId) => GetPendingOrdersHandler(workplaceId);

	// GET: api/orders/workplaces/{workplaceId}/pending
	[HttpGet("orders/workplaces/{workplaceId}/pending")]
	public Task<IActionResult> GetPendingOrders(Guid workplaceId) => GetPendingOrdersHandler(workplaceId);

	// GET: api/orders/active?workplaceId=...
	[HttpGet("orders/active")]
	public Task<IActionResult> GetActiveOrdersCompatible([FromQuery] Guid workplaceId) => GetActiveOrdersHandler(workplaceId);

	// GET: api/orders/workplaces/{workplaceId}/active
	[HttpGet("orders/workplaces/{workplaceId}/active")]
	public Task<IActionResult> GetActiveOrders(Guid workplaceId) => GetActiveOrdersHandler(workplaceId);

	// GET: api/orders/in-work?workplaceId=...
	[HttpGet("orders/in-work")]
	public Task<IActionResult> GetActiveAndPendingOrdersCompatible([FromQuery] Guid workplaceId) => GetActiveAndPendingOrdersHandler(workplaceId);

	// GET: api/orders/workplaces/{workplaceId}/in-work
	[HttpGet("orders/workplaces/{workplaceId}/in-work")]
	public Task<IActionResult> GetActiveAndPendingOrders(Guid workplaceId) => GetActiveAndPendingOrdersHandler(workplaceId);

	// POST: api/orders
	[HttpPost("orders")]
	[HttpPost("orders/create")]
	public Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request) => CreateOrderHandler(request);

	// POST: api/orders/operations/start
	[HttpPost("orders/operations/start")]
	[HttpPost("operations/start")]
	public Task<IActionResult> BeginOrderWorkplace([FromBody] BeginWorkplaceRequestDto request) => BeginOrderWorkplaceHandler(request);

	// POST: api/orders/operations/complete
	[HttpPost("orders/operations/complete")]
	[HttpPost("operations/complete")]
	public Task<IActionResult> CompleteOrderWorkplace([FromBody] CompleteWorkplaceRequestDto request) => CompleteOrderWorkplaceHandler(request);

	// PUT: api/orders/footprint/{productionOrderId}/workplace/{workplaceId}
	[HttpPut("orders/footprint/{productionOrderId}/workplace/{workplaceId}")]
	[HttpPut("traces/{productionOrderId}/workplace/{workplaceId}")]
	public Task<IActionResult> SetOrderFootprintStatus(Guid productionOrderId, Guid workplaceId, [FromBody] SetFootprintStatusRequestDto request)
		=> SetOrderFootprintStatusHandler(productionOrderId, workplaceId, request);

	// PUT: api/orders/footprint/{productionOrderId}/batch
	[HttpPut("orders/footprint/{productionOrderId}/batch")]
	public Task<IActionResult> UpdateOrderFootprintBatch(Guid productionOrderId, [FromBody] UpdateFootprintBatchRequest request)
		=> UpdateOrderFootprintBatchHandler(productionOrderId, request);

	// PUT: api/orders/{orderId}/comments/{commentId}
	[HttpPut("orders/{orderId}/comments/{commentId}")]
	public Task<IActionResult> UpdateOrderComment(Guid orderId, Guid commentId, [FromBody] UpdateCommentRequestDto request)
		=> UpdateOrderCommentHandler(orderId, commentId, request);

	// GET: api/orders/{identifier}/trace  
	[HttpGet("orders/{identifier}/trace")]
	public Task<IActionResult> GetOrderTrace(string identifier) => GetOrderTraceHandler(identifier);

	// GET: api/orders/{orderId}/comments
	[HttpGet("orders/{orderId}/comments")]
	public Task<IActionResult> GetOrderComments(Guid orderId) => GetOrderCommentsHandler(orderId);

	// POST: api/orders/{orderId}/comments
	[HttpPost("orders/{orderId}/comments")]
	public Task<IActionResult> AddOrderComment(Guid orderId, [FromBody] AddCommentRequestDto request) => AddOrderCommentHandler(orderId, request);

	// GET: api/orders/{identifier}
	[HttpGet("orders/{identifier}")]
	public Task<IActionResult> GetOrderByIdentifier(string identifier) => GetOrderByIdentifierHandler(identifier);

	// POST: api/orders/{orderId}/productionOrderComments
	[HttpPost("orders/{orderId}/productionOrderComments")]
	public Task<IActionResult> AddProductionOrderComment( Guid orderId, [FromBody] AddProductionOrderCommentRequestDto request)
		=> AddProductionOrderCommentHandler(orderId, request);

	// POST: api/orders/{orderId}/OrderSupplyComments
	[HttpPost("orders/{orderId}/OrderSupplyComments")]
	public Task<IActionResult> AddSupplyComment(Guid orderId, [FromBody] AddSupplyCommentRequestDto request) => AddSupplyCommentHandler(orderId, request);

	// POST: api/orders/{orderId}/complete
	[HttpPost("orders/{orderId}/complete")]
	public Task<IActionResult> OrderComplete(Guid orderId) => SetOrderCompleteHandler(orderId);

	// POST: api/orders/{orderId}/departure
	[HttpPost("orders/{orderId}/departure")]
	public Task<IActionResult> OrderDeparture(Guid orderId) => SetOrderDepartureHandler(orderId);

	// GET: api/orders/{orderId}/commercial
	[HttpGet("orders/{orderId}/commercial")]
	public async Task<IActionResult> GetOrderCommercial(Guid orderId)
	{
		var commercial = await _orderService.GetOrderCommercialAsync(orderId);
		return Ok(commercial);
	}

	// PUT: api/orders/{orderId}/commercial
	[HttpPut("orders/{orderId}/commercial")]
	public async Task<IActionResult> UpdateOrderCommercial(
		Guid orderId,
		[FromBody] OrderCommercialRequestDto request)
	{
		var result = await _orderService.UpdateOrderCommercialAsync(orderId, request);
		return Ok(result);
	}
} 