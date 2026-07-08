// KG.MES.Server/Controllers/SalesController.cs
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

public partial class SalesController : ControllerBase
{
	private readonly IOrderService _orderService;
	private readonly ILogger<SalesController> _logger;

	public SalesController(IOrderService orderService, ILogger<SalesController> logger)
	{
		_orderService = orderService;
		_logger = logger;
	}

	public async Task<IActionResult> GetSalesOrdersHandler(int page = 1, int limit = 50, string? sortBy = "ready_date",
			string? sortOrder = "asc", string? orderNumber = null,
			Guid? workplaceId = null, List<Guid>? workplaceIds = null,
			string? customerName = null, Guid? managerId = null)
	{
		if (workplaceId.HasValue)
		{
			workplaceIds ??= [];
			workplaceIds.Add(workplaceId.Value);
		}

		var result = await _orderService.GetSalesOrdersAsync(
			page, limit, sortBy, sortOrder, workplaceIds, orderNumber);

		return Ok(result);
	}

	public async Task<IActionResult> GetSalesOrderHandler(Guid orderId)
	{
		//var order = await _orderService.GetSalesOrderByIdAsync(orderId);
		//if (order == null)
		//	return NotFound(new { error = "Order not found" });

		//return Ok(order);
		return Ok();
	}

	public async Task<IActionResult> GetSalesOrderByNumberHandler(string orderNumber)
	{
		//var order = await _orderService.GetSalesOrderByNumberAsync(orderNumber);
		//if (order == null)
		//	return NotFound(new { error = "Order not found" });

		//return Ok(order);
		return Ok();
	}

	public async Task<IActionResult> GetCustomersHandler([FromQuery] string? search = null)
	{
		var customers = await _orderService.GetCustomersAsync(search);
		return Ok(customers);
	}

	public async Task<IActionResult> GetCustomerHandler(Guid customerId)
	{
		//var customer = await _orderService.GetCustomerByIdAsync(customerId);
		//if (customer == null)
		//	return NotFound(new { error = "Customer not found" });

		//return Ok(customer);
		return Ok();
	}

	// POST: api/sales/customers
	//[HttpPost("customers")]
	//public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
	//{
	//	var customer = await _orderService.CreateCustomerAsync(request);
	//	return CreatedAtAction(nameof(GetCustomer), new { customerId = customer.Id }, customer);
	//}

	public async Task<IActionResult> GetOrderCommercialHandler(Guid orderId)
	{
		var commercial = await _orderService.GetOrderCommercialAsync(orderId);
		if (commercial == null)
			return NotFound(new { error = "Commercial info not found" });

		return Ok(commercial);
	}

	public async Task<IActionResult> UpdateOrderCommercialHandler(
		Guid orderId,
		[FromBody] OrderCommercialRequestDto request)
	{
		//var result = await _orderService.UpsertOrderCommercialAsync(orderId, request);
		//return Ok(result);
		return Ok();
	}

	public async Task<IActionResult> GetManagersHandler()
	{
		//var managers = await _orderService.GetSalesManagersAsync();
		//return Ok(managers);
		return Ok();
	}
}