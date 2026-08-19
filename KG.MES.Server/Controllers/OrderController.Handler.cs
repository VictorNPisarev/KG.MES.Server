
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

/// <summary>
/// Обработчики запросов
/// </summary>
public partial class OrderController
{
	private readonly IOrderService orderService;
	private readonly ILogger<OrderController> logger;

	public OrderController(IOrderService orderService, ILogger<OrderController> logger)
	{
		this.orderService = orderService;
		this.logger = logger;
	}


	public async Task<IActionResult> GetOrdersHandler(int page = 1, int limit = 50, string? sortBy = "ready_date",
		string? sortOrder = "asc", string? orderNumber = null, Guid? workplaceId = null, List<Guid> ? workplaceIds = null)
	{
		if (workplaceId.HasValue)
		{
			workplaceIds ??= [];
			workplaceIds.Add(workplaceId.Value);
		}

		var result = await orderService.GetOrdersAsync(page, limit, sortBy, sortOrder, workplaceIds, orderNumber);

		return Ok(result);
	}

	public async Task<IActionResult> GetPendingOrdersHandler(Guid workplaceId)
	{
		if (workplaceId == Guid.Empty)
			return BadRequest(new { error = "workplaceId is required" });

		var orders = await orderService.GetPendingOrdersForWorkplaceAsync(workplaceId);

		return Ok(orders);
	}

	public async Task<IActionResult> GetActiveOrdersHandler(Guid workplaceId)
	{
		if (workplaceId == Guid.Empty)
			return BadRequest(new { error = "workplaceId is required" });

		var orders = await orderService.GetActiveOrdersForWorkplaceAsync(workplaceId);

		return Ok(orders);
	}

	public async Task<IActionResult> GetActiveAndPendingOrdersHandler(Guid workplaceId)
	{
		Console.WriteLine($"workplaceId: {workplaceId}");
		if (workplaceId == Guid.Empty)
			return BadRequest(new { error = "workplaceId is required" });

		var orders = await orderService.GetActiveAndPendingOrdersForWorkplaceAsync(workplaceId);

		return Ok(orders);
	}

	public async Task<IActionResult> CreateOrderHandler(OrderRequestDto request)
	{
		if (string.IsNullOrEmpty(request.OrderNumber))
			return BadRequest(new { error = "orderNumber is required" });

		var result = await orderService.CreateOrderAsync(request);

		return Ok(result);
	}

	public async Task<IActionResult> BeginOrderWorkplaceHandler(BeginWorkplaceRequestDto request)
	{
		if (request.ProductionOrderId == Guid.Empty || request.WorkplaceId == Guid.Empty || request.UserId == Guid.Empty)
			return BadRequest(new { error = "productionOrderId, workplaceId, and userId are required" });

		var result = await orderService.BeginOrderWorkplaceAsync(
			request.ProductionOrderId, request.WorkplaceId, request.UserId, request.Notes ?? " ", request.Source ?? "API");

		return Ok(result);
	}

	public async Task<IActionResult> CompleteOrderWorkplaceHandler(CompleteWorkplaceRequestDto request)
	{
		if (request.ProductionOrderId == Guid.Empty || request.WorkplaceId == Guid.Empty || request.UserId == Guid.Empty)
			return BadRequest(new { error = "productionOrderId, workplaceId, and userId are required" });
		var result = await orderService.CompleteOrderWorkplaceAsync(
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
		var result = await orderService.SetOrderFootprintStatusAsync(
			productionOrderId, workplaceId, request.Status, request.UserId, request.Notes ?? " ");
		return Ok(result);
	}

	public async Task<IActionResult> UpdateOrderFootprintBatchHandler(Guid productionOrderId, UpdateFootprintBatchRequest request)
	{
		if (request.Footprints == null || request.Footprints.Count == 0)
			return BadRequest(new { error = "footprints array is required" });
		var result = await orderService.UpdateOrderFootprintBatchAsync(
			productionOrderId, request.Footprints, request.UserId, request.Notes ?? " ");
		return Ok(result);
	}

	public async Task<IActionResult> UpdateOrderCommentHandler(Guid orderId, Guid commentId, UpdateCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await orderService.UpdateOrderCommentAsync(orderId, commentId, request.Content);
		return Ok(result);
	}

	public async Task<IActionResult> GetOrderTraceHandler(string identifier)
	{
		var traces = await orderService.GetOrderTraceByNumberAsync(identifier);
		
		if (traces == null || traces.Count == 0)
			return NotFound(new { error = "Order not found" });

		return Ok(new { orders = traces });
	}

	public async Task<IActionResult> GetOrderCommentsHandler(Guid orderId)
	{
		var comments = await orderService.GetOrderCommentsAsync(orderId);
		return Ok(comments);
	}

	public async Task<IActionResult> AddOrderCommentHandler(Guid orderId, AddCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await orderService.AddOrderCommentAsync(orderId, request.UserId, request.Content);
		return Ok(result);
	}

	public async Task<IActionResult> GetOrderByIdentifierHandler(string identifier)
	{
		var isUuid = Guid.TryParse(identifier, out var orderId);
		var order = isUuid
			? await orderService.GetOrderByIdAsync(orderId)
			: await orderService.GetOrderByNumberAsync(identifier);

		if (order == null)
			return NotFound(new { error = "Order not found" });
		return Ok(order);
	}

	public async Task<IActionResult> AddProductionOrderCommentHandler(Guid orderId, AddProductionOrderCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await orderService.AddProductionOrderCommentAsync(
			orderId, request.ProductionOrderId, request.UserId, request.Content);

		return Ok(result);
	}

	public async Task<IActionResult> AddSupplyCommentHandler(Guid orderId, AddSupplyCommentRequestDto request)
	{
		if (string.IsNullOrEmpty(request.Content))
			return BadRequest(new { error = "content is required" });

		var result = await orderService.AddSupplyCommentAsync(
			orderId, request.SupplyTypeId, request.UserId, request.Content);

		return Ok(result);
	}

		public async Task<IActionResult> SetOrderCompleteHandler(Guid orderId)
	{
		var result = await orderService.SetOrderCompleteAsync(orderId, null, null);
		return Ok(result);
	}

	public async Task<IActionResult> SetOrderDepartureHandler(Guid orderId)
	{
		var result = await orderService.SetOrderDepartureAsync(orderId, null, null);
		return Ok(result);
	}

	public async Task<IActionResult> GetOrderCommercialHandler(Guid orderId)
	{
		var commercial = await orderService.GetOrderCommercialAsync(orderId);
		return Ok(commercial);
	}

	public async Task<IActionResult> UpdateOrderCommercialHandler(
		Guid orderId,
		[FromBody] OrderCommercialRequestDto request)
	{
		var result = await orderService.UpdateOrderCommercialAsync(orderId, request);
		return Ok(result);
	}

	public async Task<IActionResult> GetOrderForEditHandler(Guid orderId)
	{
		var result = await orderService.GetOrderForEditAsync(orderId);
		if (result == null)
			return NotFound(new { error = "Order not found" });
		return Ok(result);
	}

	public async Task<IActionResult> UpdateOrderHandler(Guid orderId, [FromBody] OrderRequestDto dto)
	{
		if (dto == null)
			return BadRequest(new { error = "Request body is required" });

		var result = await orderService.UpdateOrderAsync(orderId, dto);
		if (!result)
			return NotFound(new { error = "Order not found or update failed" });

		return Ok(new { success = true, message = "Order updated" });
	}

	public async Task<IActionResult> DeleteOrderHandler(Guid orderId)
	{
		var result = await orderService.DeleteOrderAsync(orderId);
		if (!result)
			return NotFound(new { error = "Order not found or delete failed" });

		return Ok(new { success = true, message = "Order deleted" });
	}
}