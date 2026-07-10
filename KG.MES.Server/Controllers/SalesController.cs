// KG.MES.Server/Controllers/SalesController.cs
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public partial class SalesController : ControllerBase
{
	// GET: api/sales/orders
	[HttpGet("orders")]
	public Task<IActionResult> GetSalesOrders([FromQuery] int page = 1, [FromQuery] int limit = 50, [FromQuery] string? sortBy = "ready_date",
			[FromQuery] string? sortOrder = "asc", [FromQuery] string? orderNumber = null,
			[FromQuery] Guid? workplaceId = null, [FromQuery] List<Guid>? workplaceIds = null,
			[FromQuery] string? customerName = null, [FromQuery] Guid? managerId = null)
		=> GetSalesOrdersHandler(page,limit, sortBy, sortOrder, orderNumber, workplaceId, workplaceIds, customerName, managerId);

	// GET: api/sales/orders/{orderId}
	[HttpGet("orders/{orderId}")]
	public async Task<IActionResult> GetSalesOrder(Guid orderId)
	{
		//var order = await _orderService.GetSalesOrderByIdAsync(orderId);
		//if (order == null)
		//	return NotFound(new { error = "Order not found" });

		//return Ok(order);
		return Ok();
	}

	// GET: api/sales/orders/by-number/{orderNumber}
	[HttpGet("orders/by-number/{orderNumber}")]
	public async Task<IActionResult> GetSalesOrderByNumber(string orderNumber)
	{
		//var order = await _orderService.GetSalesOrderByNumberAsync(orderNumber);
		//if (order == null)
		//	return NotFound(new { error = "Order not found" });

		//return Ok(order);
		return Ok();
	}

	// GET: api/sales/customers
	[HttpGet("customers")]
	public async Task<IActionResult> GetCustomers([FromQuery] string? search = null)
	{
		var customers = await _orderService.GetCustomersAsync(search);
		return Ok(customers);
	}

	// GET: api/sales/customers/{customerId}
	[HttpGet("customers/{customerId}")]
	public async Task<IActionResult> GetCustomer(Guid customerId)
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

	// GET: api/sales/orders/{orderId}/commercial
	[HttpGet("orders/{orderId}/commercial")]
	public async Task<IActionResult> GetOrderCommercial(Guid orderId)
	{
		var commercial = await _orderService.GetOrderCommercialAsync(orderId);
		if (commercial == null)
			return NotFound(new { error = "Commercial info not found" });

		return Ok(commercial);
	}

	// PUT: api/sales/orders/{orderId}/commercial
	[HttpPut("orders/{orderId}/commercial")]
	public async Task<IActionResult> UpdateOrderCommercial(
		Guid orderId,
		[FromBody] OrderCommercialRequestDto request)
	{
		//var result = await _orderService.UpsertOrderCommercialAsync(orderId, request);
		//return Ok(result);
		return Ok();
	}

	// GET: api/sales/managers
	[HttpGet("managers")]
	public async Task<IActionResult> GetManagers()
	{
		//var managers = await _orderService.GetSalesManagersAsync();
		//return Ok(managers);
		return Ok();
	}
}