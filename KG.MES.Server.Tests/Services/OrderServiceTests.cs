using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace KG.MES.Server.Tests.Services;

[Trait("Category", "Unit")]
public class OrderServiceTests : IDisposable
{
	private readonly AppDbContext _context;
	private readonly OrderService _service;
	private readonly Mock<ILogger<OrderService>> _loggerMock;
	private readonly OrderAttributeService _attributeService;

	public OrderServiceTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}")
			.ConfigureWarnings(warnings =>
				warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)) // ← Добавлено!
			.Options;

		_context = new AppDbContext(options);

		_loggerMock = new Mock<ILogger<OrderService>>();
		_attributeService = new OrderAttributeService(_context,
			new Mock<ILogger<OrderAttributeService>>().Object);

		_service = new OrderService(_context, _loggerMock.Object, _attributeService);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task CreateOrderAsync_ShouldCreateOrderWithAllRelatedEntities()
	{
		// Arrange
		var noneId = Guid.NewGuid();
		var supplyType1 = new SupplyType { Id = Guid.NewGuid(), Name = "lumber", IsActive = true };
		var supplyType2 = new SupplyType { Id = Guid.NewGuid(), Name = "glass", IsActive = true };

		_context.Workplaces.Add(new Workplace
		{
			Id = noneId,
			Name = "none",
			Code = "NONE",
			IsWorkplace = false
		});
		_context.SupplyTypes.AddRange(supplyType1, supplyType2);
		await _context.SaveChangesAsync();

		var request = new OrderRequestDto
		{
			OrderNumber = "TEST-001",
			WindowCount = 5,
			WindowArea = 12.5m,
			PlateCount = 0,
			PlateArea = 0,
			IsEconom = false,
			IsClaim = false,
			IsOnlyPaid = false,
			Comment = "Test comment",
			Lumber = "Pine",
			Machine = "Conturex"
		};

		// Act
		var result = await _service.CreateOrderAsync(request);

		// Assert
		result.Success.Should().BeTrue();
		result.OrderId.Should().NotBeEmpty();
		result.ProductionOrderId.Should().NotBeEmpty();
		result.OrderSupplyId.Should().NotBeEmpty();
		result.SupplyItemIds.Should().HaveCount(2);

		// Проверяем, что заказ создался
		var order = await _context.Orders.FindAsync(result.OrderId);
		order.Should().NotBeNull();
		order!.OrderNumber.Should().Be("TEST-001");
		order.WindowCount.Should().Be(5);

		// Проверяем, что ProductionOrder создался
		var productionOrder = await _context.ProductionOrders.FindAsync(result.ProductionOrderId);
		productionOrder.Should().NotBeNull();
		productionOrder!.Lumber.Should().Be("Pine");
		productionOrder.Machine.Should().Be("Conturex");
		productionOrder.CurrentWorkplaceId.Should().Be(noneId);

		// Проверяем, что SupplyItems создались
		var supplyItems = await _context.SupplyItems
			.Where(si => si.OrderSupplyId == result.OrderSupplyId)
			.ToListAsync();
		supplyItems.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetOrdersAsync_ShouldReturnPaginatedOrders()
	{
		// Arrange
		var workplaceId = Guid.NewGuid();
		var order1 = new Order
		{
			Id = Guid.NewGuid(),
			OrderNumber = "001",
			ReadyDate = DateTime.UtcNow.AddDays(1),
			WindowCount = 5
		};
		var order2 = new Order
		{
			Id = Guid.NewGuid(),
			OrderNumber = "002",
			ReadyDate = DateTime.UtcNow.AddDays(2),
			WindowCount = 10
		};

		_context.Workplaces.Add(new Workplace { Id = workplaceId, Name = "Test", IsWorkplace = true });
		_context.Orders.AddRange(order1, order2);
		_context.ProductionOrders.AddRange(
			new ProductionOrder { OrderId = order1.Id, CurrentWorkplaceId = workplaceId },
			new ProductionOrder { OrderId = order2.Id, CurrentWorkplaceId = workplaceId }
		);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetOrdersAsync(1, 10, "ready_date", "asc", null, null);

		// Assert
		result.Should().NotBeNull();
		result.Data.Should().HaveCount(2);
		result.Pagination.Total.Should().Be(2);
		result.Pagination.Page.Should().Be(1);
	}

	[Fact]
	public async Task GetOrdersAsync_WithOrderNumberFilter_ShouldReturnFilteredOrders()
	{
		// Arrange
		var workplaceId = Guid.NewGuid();
		var order1 = new Order { Id = Guid.NewGuid(), OrderNumber = "001", ReadyDate = DateTime.UtcNow };
		var order2 = new Order { Id = Guid.NewGuid(), OrderNumber = "002", ReadyDate = DateTime.UtcNow };

		_context.Workplaces.Add(new Workplace { Id = workplaceId, Name = "Test", IsWorkplace = true });
		_context.Orders.AddRange(order1, order2);
		_context.ProductionOrders.AddRange(
			new ProductionOrder { OrderId = order1.Id, CurrentWorkplaceId = workplaceId },
			new ProductionOrder { OrderId = order2.Id, CurrentWorkplaceId = workplaceId }
		);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetOrdersAsync(1, 10, "ready_date", "asc", null, "001");

		// Assert
		result.Data.Should().HaveCount(1);
		result.Data[0].OrderNumber.Should().Be("001");
	}

	[Fact]
	public async Task GetOrderByIdAsync_ShouldReturnOrderDetails()
	{
		// Arrange
		var orderId = Guid.NewGuid();
		var workplaceId = Guid.NewGuid();
		var order = new Order
		{
			Id = orderId,
			OrderNumber = "TEST-001",
			ReadyDate = DateTime.UtcNow,
			WindowCount = 5
		};
		var productionOrder = new ProductionOrder
		{
			OrderId = orderId,
			CurrentWorkplaceId = workplaceId
		};

		_context.Workplaces.Add(new Workplace { Id = workplaceId, Name = "Test", IsWorkplace = true });
		_context.Orders.Add(order);
		_context.ProductionOrders.Add(productionOrder);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetOrderByIdAsync(orderId);

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(orderId);
		result.OrderNumber.Should().Be("TEST-001");
		result.CurrentStatus.Should().Be("Test");
	}

	[Fact]
	public async Task DeleteOrderAsync_ShouldRemoveOrderAndRelatedData()
	{
		// Arrange
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();
		var orderSupplyId = Guid.NewGuid();

		var order = new Order { Id = orderId, OrderNumber = "DELETE-001" };
		var productionOrder = new ProductionOrder { Id = productionOrderId, OrderId = orderId };
		var orderSupply = new OrderSupply { Id = orderSupplyId, OrderId = orderId };

		_context.Orders.Add(order);
		_context.ProductionOrders.Add(productionOrder);
		_context.OrderSupplies.Add(orderSupply);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.DeleteOrderAsync(orderId);

		// Assert
		result.Should().BeTrue();

		var deletedOrder = await _context.Orders.FindAsync(orderId);
		deletedOrder.Should().BeNull();

		var deletedProductionOrder = await _context.ProductionOrders.FindAsync(productionOrderId);
		deletedProductionOrder.Should().BeNull();

		var deletedOrderSupply = await _context.OrderSupplies.FindAsync(orderSupplyId);
		deletedOrderSupply.Should().BeNull();
	}

	[Fact]
	public async Task DeleteOrderAsync_WithNonExistentOrder_ShouldReturnFalse()
	{
		// Act
		var result = await _service.DeleteOrderAsync(Guid.NewGuid());

		// Assert
		result.Should().BeFalse();
	}
}