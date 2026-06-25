
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

/// <summary>
/// Обработчики запросов
/// </summary>
public partial class OrderController
{
	private readonly IOrderService _orderService;
	private readonly ILogger<OrderController> _logger;

	public OrderController(IOrderService orderService, ILogger<OrderController> logger)
	{
		_orderService = orderService;
		_logger = logger;
	}


	public async Task<IActionResult> GetOrdersHandler(int page = 1, int limit = 50, string? sortBy = "ready_date",
		string? sortOrder = "asc", Guid? workplaceId = null, string? orderNumber = null)
	{
		var result = await _orderService.GetOrdersAsync(page, limit, sortBy, sortOrder, workplaceId, orderNumber);

		return Ok(result);
	}

	public async Task<IActionResult> GetPendingOrdersHandler(Guid workplaceId)
	{
		if (workplaceId == Guid.Empty)
			return BadRequest(new { error = "workplaceId is required" });

		var orders = await _orderService.GetPendingOrdersForWorkplaceAsync(workplaceId);

		return Ok(orders);
	}

	public async Task<IActionResult> GetActiveOrdersHandler(Guid workplaceId)
	{
		if (workplaceId == Guid.Empty)
			return BadRequest(new { error = "workplaceId is required" });

		var orders = await _orderService.GetActiveOrdersForWorkplaceAsync(workplaceId);

		return Ok(orders);
	}

	public async Task<IActionResult> GetActiveAndPendingOrdersHandler(Guid workplaceId)
	{
		Console.WriteLine($"workplaceId: {workplaceId}");
		if (workplaceId == Guid.Empty)
			return BadRequest(new { error = "workplaceId is required" });

		var orders = await _orderService.GetActiveAndPendingOrdersForWorkplaceAsync(workplaceId);

		return Ok(orders);
	}

	public async Task<IActionResult> CreateOrderHandler(CreateOrderRequestDto request)
	{
		if (string.IsNullOrEmpty(request.OrderNumber))
			return BadRequest(new { error = "orderNumber is required" });

		var result = await _orderService.CreateOrderAsync(request);

		return Ok(result);
	}

	public async Task<IActionResult> BeginOrderWorkplaceHandler(BeginWorkplaceRequestDto request)
	{
		if (request.ProductionOrderId == Guid.Empty || request.WorkplaceId == Guid.Empty || request.UserId == Guid.Empty)
			return BadRequest(new { error = "productionOrderId, workplaceId, and userId are required" });

		var result = await _orderService.BeginOrderWorkplaceAsync(
			request.ProductionOrderId, request.WorkplaceId, request.UserId, request.Notes ?? " ", request.Source ?? "API");

		return Ok(result);
	}

	public async Task<IActionResult> CompleteOrderWorkplaceHandler(CompleteWorkplaceRequestDto request)
	{
		if (request.ProductionOrderId == Guid.Empty || request.WorkplaceId == Guid.Empty || request.UserId == Guid.Empty)
			return BadRequest(new { error = "productionOrderId, workplaceId, and userId are required" });
		var result = await _orderService.CompleteOrderWorkplaceAsync(
			request.ProductionOrderId, request.WorkplaceId, request.UserId, request.Notes ?? " ", request.Source ?? "API");
		return Ok(result);
	}

	public async Task<IActionResult> SetOrderFootprintStatusHandler(
		Guid productionOrderId,
		Guid workplaceId,
		SetFootprintStatusRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Status))
			return BadRequest(new { error = "status is required" });
		var result = await _orderService.SetOrderFootprintStatusAsync(
			productionOrderId, workplaceId, request.Status, request.UserId, request.Notes ?? " ");
		return Ok(result);
	}

	public async Task<IActionResult> UpdateOrderFootprintBatchHandler(Guid productionOrderId, UpdateFootprintBatchRequest request)
	{
		if (request.Footprints == null || request.Footprints.Count == 0)
			return BadRequest(new { error = "footprints array is required" });
		var result = await _orderService.UpdateOrderFootprintBatchAsync(
			productionOrderId, request.Footprints, request.UserId, request.Notes ?? " ");
		return Ok(result);
	}

	public async Task<IActionResult> UpdateOrderCommentHandler(Guid orderId, Guid commentId, UpdateCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await _orderService.UpdateOrderCommentAsync(orderId, commentId, request.Content);
		return Ok(result);
	}

	public async Task<IActionResult> GetOrderTraceHandler(string identifier)
	{
		var traces = await _orderService.GetOrderTraceByNumberAsync(identifier);
		if (traces == null || traces.Count == 0)
			return NotFound(new { error = "Order not found" });
		return Ok(new { orders = traces });
	}

	public async Task<IActionResult> GetOrderCommentsHandler(Guid orderId)
	{
		var comments = await _orderService.GetOrderCommentsAsync(orderId);
		return Ok(comments);
	}

	public async Task<IActionResult> AddOrderCommentHandler(Guid orderId, AddCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await _orderService.AddOrderCommentAsync(orderId, request.UserId, request.Content);
		return Ok(result);
	}

	public async Task<IActionResult> GetOrderByIdentifierHandler(string identifier)
	{
		var isUuid = Guid.TryParse(identifier, out var orderId);
		var order = isUuid
			? await _orderService.GetOrderByIdAsync(orderId)
			: await _orderService.GetOrderByNumberAsync(identifier);

		if (order == null)
			return NotFound(new { error = "Order not found" });
		return Ok(order);
	}

	public async Task<IActionResult> AddProductionOrderCommentHandler(Guid orderId, AddProductionOrderCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await _orderService.AddProductionOrderCommentAsync(
			orderId, request.ProductionOrderId, request.UserId, request.Content);

		return Ok(result);
	}

	public async Task<IActionResult> AddSupplyCommentHandler(Guid orderId, AddSupplyCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await _orderService.AddSupplyCommentAsync(
			orderId, request.SupplyTypeId, request.UserId, request.Content);

		return Ok(result);
	}

		public async Task<IActionResult> SetOrderCompleteHandler(Guid orderId)
	{
		var result = await _orderService.SetOrderCompleteAsync(orderId, null, null);
		return Ok(result);
	}

	public async Task<IActionResult> SetOrderDepartureHandler(Guid orderId)
	{
		var result = await _orderService.SetOrderDepartureAsync(orderId, null, null);
		return Ok(result);
	}


}