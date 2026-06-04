using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api")]
public class OrdersController : ControllerBase
{
	private readonly IOrderService _orderService;
	private readonly ILogger<OrdersController> _logger;

	public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
	{
		_orderService = orderService;
		_logger = logger;
	}

	// ==========================================
	// 1. СНАЧАЛА ВСЕ СТАТИЧЕСКИЕ МАРШРУТЫ
	// ==========================================

	// GET: api/orders
	[HttpGet("orders")]
	public async Task<IActionResult> GetOrders(
		[FromQuery] int page = 1,
		[FromQuery] int limit = 50,
		[FromQuery] string? sortBy = "ready_date",
		[FromQuery] string? sortOrder = "asc",
		[FromQuery] Guid? workplaceId = null,
		[FromQuery] string? orderNumber = null)
	{
		var result = await _orderService.GetOrdersAsync(page, limit, sortBy, sortOrder, workplaceId, orderNumber);
		return Ok(result);
	}

	// GET: api/orders/pending?workplaceId=...
	[HttpGet("orders/pending")]
	public async Task<IActionResult> GetPendingOrdersCompatible([FromQuery] Guid workplaceId)
	{
		if (workplaceId == Guid.Empty)
			return BadRequest(new { error = "workplaceId is required" });

		return await GetPendingOrders(workplaceId);
	}

	// GET: api/orders/active?workplaceId=...
	[HttpGet("orders/active")]
	public async Task<IActionResult> GetActiveOrdersCompatible([FromQuery] Guid workplaceId)
	{
		if (workplaceId == Guid.Empty)
			return BadRequest(new { error = "workplaceId is required" });

		return await GetActiveOrders(workplaceId);
	}

	// GET: api/orders/in-work?workplaceId=...
	[HttpGet("orders/in-work")]
	public async Task<IActionResult> GetActiveAndPendingOrdersCompatible([FromQuery] Guid workplaceId)
	{
		if (workplaceId == Guid.Empty)
			return BadRequest(new { error = "workplaceId is required" });

		return await GetActiveAndPendingOrders(workplaceId);
	}

	// GET: api/orders/workplaces/{workplaceId}/pending
	[HttpGet("orders/workplaces/{workplaceId}/pending")]
	public async Task<IActionResult> GetPendingOrders(Guid workplaceId)
	{
		var orders = await _orderService.GetPendingOrdersForWorkplaceAsync(workplaceId);
		return Ok(orders);
	}

	// GET: api/orders/workplaces/{workplaceId}/active
	[HttpGet("orders/workplaces/{workplaceId}/active")]
	public async Task<IActionResult> GetActiveOrders(Guid workplaceId)
	{
		var orders = await _orderService.GetActiveOrdersForWorkplaceAsync(workplaceId);
		return Ok(orders);
	}

	// GET: api/orders/workplaces/{workplaceId}/in-work
	[HttpGet("orders/workplaces/{workplaceId}/in-work")]
	public async Task<IActionResult> GetActiveAndPendingOrders(Guid workplaceId)
	{
		if (workplaceId == Guid.Empty)
			return BadRequest(new { error = "workplaceId is required" });

		var orders = await _orderService.GetActiveAndPendingOrdersForWorkplaceAsync(workplaceId);
		return Ok(orders);
	}

	// POST: api/orders
	[HttpPost("orders")]
	public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
	{
		if (string.IsNullOrEmpty(request.OrderNumber))
			return BadRequest(new { error = "orderNumber is required" });
		var result = await _orderService.CreateOrderAsync(request);
		return Ok(result);
	}

	// POST: api/orders/operations/start
	[HttpPost("orders/operations/start")]
	public async Task<IActionResult> BeginOrderWorkplace([FromBody] BeginWorkplaceRequestDto request)
	{
		if (request.ProductionOrderId == Guid.Empty || request.WorkplaceId == Guid.Empty || request.UserId == Guid.Empty)
			return BadRequest(new { error = "productionOrderId, workplaceId, and userId are required" });
		var result = await _orderService.BeginOrderWorkplaceAsync(
			request.ProductionOrderId, request.WorkplaceId, request.UserId, request.Notes ?? " ", request.Source ?? "API");
		return Ok(result);
	}

	// POST: api/orders/operations/complete
	[HttpPost("orders/operations/complete")]
	public async Task<IActionResult> CompleteOrderWorkplace([FromBody] CompleteWorkplaceRequestDto request)
	{
		if (request.ProductionOrderId == Guid.Empty || request.WorkplaceId == Guid.Empty || request.UserId == Guid.Empty)
			return BadRequest(new { error = "productionOrderId, workplaceId, and userId are required" });
		var result = await _orderService.CompleteOrderWorkplaceAsync(
			request.ProductionOrderId, request.WorkplaceId, request.UserId, request.Notes ?? " ", request.Source ?? "API");
		return Ok(result);
	}

	// PUT: api/orders/footprint/{productionOrderId}/workplace/{workplaceId}
	[HttpPut("orders/footprint/{productionOrderId}/workplace/{workplaceId}")]
	public async Task<IActionResult> SetOrderFootprintStatus(
		Guid productionOrderId,
		Guid workplaceId,
		[FromBody] SetFootprintStatusRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Status))
			return BadRequest(new { error = "status is required" });
		var result = await _orderService.SetOrderFootprintStatusAsync(
			productionOrderId, workplaceId, request.Status, request.UserId, request.Notes ?? " ");
		return Ok(result);
	}

	// PUT: api/orders/footprint/{productionOrderId}/batch
	[HttpPut("orders/footprint/{productionOrderId}/batch")]
	public async Task<IActionResult> UpdateOrderFootprintBatch(
		Guid productionOrderId,
		[FromBody] UpdateFootprintBatchRequest request)
	{
		if (request.Footprints == null || request.Footprints.Count == 0)
			return BadRequest(new { error = "footprints array is required" });
		var result = await _orderService.UpdateOrderFootprintBatchAsync(
			productionOrderId, request.Footprints, request.UserId, request.Notes ?? " ");
		return Ok(result);
	}

	// PUT: api/orders/{orderId}/comments/{commentId}
	[HttpPut("orders/{orderId}/comments/{commentId}")]
	public async Task<IActionResult> UpdateOrderComment(Guid orderId, Guid commentId, [FromBody] UpdateCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await _orderService.UpdateOrderCommentAsync(orderId, commentId, request.Content);
		return Ok(result);
	}


	// ==========================================
	// 2. В САМОМ КОНЦЕ — МАРШРУТЫ С ПАРАМЕТРАМИ
	// ==========================================

	// GET: api/orders/{identifier}/trace   ← ТОЖЕ СТАТИЧЕСКИЙ СУФФИКС, НО ВАЖЕН ПОРЯДОК
	[HttpGet("orders/{identifier}/trace")]
	public async Task<IActionResult> GetOrderTrace(string identifier)
	{
		var traces = await _orderService.GetOrderTraceByNumberAsync(identifier);
		if (traces == null || traces.Count == 0)
			return NotFound(new { error = "Order not found" });
		return Ok(new { orders = traces });
	}

	// GET: api/orders/{orderId}/comments
	[HttpGet("orders/{orderId}/comments")]
	public async Task<IActionResult> GetOrderComments(Guid orderId)
	{
		var comments = await _orderService.GetOrderCommentsAsync(orderId);
		return Ok(comments);
	}
	// POST: api/orders/{orderId}/comments
	[HttpPost("orders/{orderId}/comments")]
	public async Task<IActionResult> AddOrderComment(Guid orderId, [FromBody] AddCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await _orderService.AddOrderCommentAsync(orderId, request.UserId, request.Content);
		return Ok(result);
	}

	// GET: api/orders/{identifier}   ← САМЫЙ ОБЩИЙ МАРШРУТ — В САМОМ КОНЦЕ!
	[HttpGet("orders/{identifier}")]
	public async Task<IActionResult> GetOrderByIdentifier(string identifier)
	{
		var isUuid = Guid.TryParse(identifier, out var orderId);
		var order = isUuid
			? await _orderService.GetOrderByIdAsync(orderId)
			: await _orderService.GetOrderByNumberAsync(identifier);

		if (order == null)
			return NotFound(new { error = "Order not found" });
		return Ok(order);
	}

	// POST: api/orders/{orderId}/productionOrderComments
	[HttpPost("orders/{orderId}/productionOrderComments")]
	public async Task<IActionResult> AddProductionOrderComment( Guid orderId, [FromBody] AddProductionOrderCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await _orderService.AddProductionOrderCommentAsync(
			orderId, request.ProductionOrderId, request.UserId, request.Content);

		return Ok(result);
	}

	// POST: api/orders/{orderId}/OrderSupplyComments
	[HttpPost("orders/{orderId}/OrderSupplyComments")]
	public async Task<IActionResult> AddSupplyComment(Guid orderId, [FromBody] AddSupplyCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await _orderService.AddSupplyCommentAsync(
			orderId, request.SupplyTypeId, request.UserId, request.Content);

		return Ok(result);
	}
}