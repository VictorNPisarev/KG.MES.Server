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

	// GET: api/Orders
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

	// GET: api/Orders/{identifier}
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

	// GET: api/Orders/trace/{orderNumber}
	[HttpGet("trace/{orderNumber}")]
	public async Task<IActionResult> GetOrderTrace(string orderNumber)
	{
		var traces = await _orderService.GetOrderTraceByNumberAsync(orderNumber);

		if (traces == null || traces.Count == 0)
			return NotFound(new { error = "Order not found" });

		return Ok(new { orders = traces });
	}

	// GET: api/Orders/workplaces/{workplaceId}/pending
	[HttpGet("workplaces/{workplaceId}/pending")]
	public async Task<IActionResult> GetPendingOrders(Guid workplaceId)
	{
		var orders = await _orderService.GetPendingOrdersForWorkplaceAsync(workplaceId);

		var enhancedOrders = orders.Select(order => new
		{
			order.ProductionOrderId,
			order.WorkplaceId,
			order.Status,
			order.OrderId,
			order.OrderNumber,
			order.WindowCount,
			order.WindowArea,
			order.PlateCount,
			order.PlateArea,
			order.ReadyDate,
			order.IsEconom,
			order.IsClaim,
			order.IsOnlyPaid,
			FromJoinery = order.Status == "joinery"
		});

		return Ok(enhancedOrders);
	}

	// GET: api/Orders/workplaces/{workplaceId}/active
	[HttpGet("workplaces/{workplaceId}/active")]
	public async Task<IActionResult> GetActiveOrders(Guid workplaceId)
	{
		var orders = await _orderService.GetActiveOrdersForWorkplaceAsync(workplaceId);
		return Ok(orders);
	}

	// GET: api/Orders/workplaces/{workplaceId}/all
	[HttpGet("workplaces/{workplaceId}/all")]
	public async Task<IActionResult> GetActiveAndPendingOrders(Guid workplaceId)
	{
		var orders = await _orderService.GetActiveAndPendingOrdersForWorkplaceAsync(workplaceId);

		var enhancedOrders = orders.Select(order => new
		{
			order.ProductionOrderId,
			order.WorkplaceId,
			order.Status,
			order.OrderId,
			order.OrderNumber,
			order.WindowCount,
			order.WindowArea,
			order.PlateCount,
			order.PlateArea,
			order.ReadyDate,
			order.IsEconom,
			order.IsClaim,
			order.IsOnlyPaid,
			WorkplaceOrderStatus = order.Status,
			FromJoinery = order.Status == "joinery",
			Name = order.Status == "joinery" ? $"🪚 {order.OrderNumber}" : order.OrderNumber
		});

		return Ok(enhancedOrders);
	}

	// POST: api/Orders
	[HttpPost]
	public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
	{
		if (string.IsNullOrEmpty(request.OrderNumber))
			return BadRequest(new { error = "orderNumber is required" });

		var result = await _orderService.CreateOrderAsync(request);
		return Ok(result);
	}

	// POST: api/Orders/operations/start
	[HttpPost("operations/start")]
	public async Task<IActionResult> BeginOrderWorkplace([FromBody] BeginWorkplaceRequestDto request)
	{
		if (request.ProductionOrderId == Guid.Empty || request.WorkplaceId == Guid.Empty || request.UserId == Guid.Empty)
			return BadRequest(new { error = "productionOrderId, workplaceId, and userId are required" });

		var result = await _orderService.BeginOrderWorkplaceAsync(
			request.ProductionOrderId, request.WorkplaceId, request.UserId, request.Notes ?? "", request.Source ?? "API");

		return Ok(result);
	}

	// POST: api/Orders/operations/complete
	[HttpPost("operations/complete")]
	public async Task<IActionResult> CompleteOrderWorkplace([FromBody] CompleteWorkplaceRequestDto request)
	{
		if (request.ProductionOrderId == Guid.Empty || request.WorkplaceId == Guid.Empty || request.UserId == Guid.Empty)
			return BadRequest(new { error = "productionOrderId, workplaceId, and userId are required" });

		var result = await _orderService.CompleteOrderWorkplaceAsync(
			request.ProductionOrderId, request.WorkplaceId, request.UserId, request.Notes ?? "", request.Source ?? "API");

		return Ok(result);
	}

	// PUT: api/Orders/footprint/{productionOrderId}/workplace/{workplaceId}
	[HttpPut("footprint/{productionOrderId}/workplace/{workplaceId}")]
	public async Task<IActionResult> SetOrderFootprintStatus(
		Guid productionOrderId,
		Guid workplaceId,
		[FromBody] SetFootprintStatusRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Status))
			return BadRequest(new { error = "status is required" });

		var result = await _orderService.SetOrderFootprintStatusAsync(
			productionOrderId, workplaceId, request.Status, request.UserId, request.Notes ?? "");

		return Ok(result);
	}

	// PUT: api/Orders/footprint/{productionOrderId}/batch
	[HttpPut("footprint/{productionOrderId}/batch")]
	public async Task<IActionResult> UpdateOrderFootprintBatch(
		Guid productionOrderId,
		[FromBody] UpdateFootprintBatchRequest request)
	{
		if (request.Footprints == null || request.Footprints.Count == 0)
			return BadRequest(new { error = "footprints array is required" });

		var result = await _orderService.UpdateOrderFootprintBatchAsync(
			productionOrderId, request.Footprints, request.UserId, request.Notes ?? "");

		return Ok(result);
	}
}
