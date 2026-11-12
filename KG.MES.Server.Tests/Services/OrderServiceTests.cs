using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using KG.MES.Server.Services;

namespace KG.MES.Server.Tests.Services;

[Trait("Category", "Unit")]
public class OrderServiceTests : IDisposable
{
	private readonly AppDbContext _context;
	private readonly OrderService _service;
	private readonly Mock<ILogger<OrderService>> _loggerMock;
	private readonly OrderAttributeService _attributeService;

	private readonly NotificationService _notificationService;

	public OrderServiceTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}")
			.ConfigureWarnings(warnings =>
				warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)) // ← Добавлено!
			.Options;

		_context = new AppDbContext(options);

		_loggerMock = new Mock<ILogger<OrderService>>();
		_attributeService = new OrderAttributeService(_context, new Mock<ILogger<OrderAttributeService>>().Object);

		_notificationService = new NotificationService(_context, new Mock<ILogger<NotificationService>>().Object);

		_service = new OrderService(_context, _loggerMock.Object, _attributeService, _notificationService);
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
		var result = await _service.GetOrdersAsync(1, 10, "ready_date", "asc", null, null, null, null);

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
		var result = await _service.GetOrdersAsync(1, 10, "ready_date", "asc", null, "001", null, null);

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

	// ====== GetOrderOperationLogsAsync ======

	[Fact]
	public async Task GetOrderOperationLogsAsync_ShouldReturnLogsSortedDescWithJoins()
	{
		// Arrange
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();
		var workplace1Id = Guid.NewGuid();
		var workplace2Id = Guid.NewGuid();
		var userId = Guid.NewGuid();
		var log1Id = Guid.NewGuid();
		var log2Id = Guid.NewGuid();
		var log3Id = Guid.NewGuid();

		var t1 = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var t2 = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);
		var t3 = new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc);

		_context.Workplaces.AddRange(
			new Workplace { Id = workplace1Id, Name = "Торцовка", Code = "CUTTING", IsWorkplace = true },
			new Workplace { Id = workplace2Id, Name = "Упаковка", Code = "PACKING", IsWorkplace = true });
		_context.Users.Add(new User { Id = userId, Email = "master@test.com", Name = "Мастер" });
		_context.Orders.Add(new Order { Id = orderId, OrderNumber = "OP-001" });
		_context.ProductionOrders.Add(new ProductionOrder { Id = productionOrderId, OrderId = orderId });
		_context.OperationLogs.AddRange(
			new OperationLog
			{
				Id = log1Id,
				ProductionOrderId = productionOrderId,
				WorkplaceId = workplace1Id,
				UserId = userId,
				OperationType = "START",
				OperationTime = t1,
				Notes = "Начало",
				Source = "API",
				CreatedAt = t1
			},
			new OperationLog
			{
				Id = log2Id,
				ProductionOrderId = productionOrderId,
				WorkplaceId = workplace1Id,
				UserId = userId,
				OperationType = "COMPLETE",
				OperationTime = t2,
				Notes = "Завершено",
				Source = "API",
				CreatedAt = t2
			},
			new OperationLog
			{
				Id = log3Id,
				ProductionOrderId = productionOrderId,
				WorkplaceId = workplace2Id,
				UserId = null,
				OperationType = "COMPLETE",
				OperationTime = t3,
				Notes = "Авто",
				Source = "auto",
				CreatedAt = t3
			});
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetOrderOperationLogsAsync("OP-001");

		// Assert
		result.Should().NotBeNull();
		result!.Should().HaveCount(3);

		// Сортировка: свежие сверху
		result[0].Id.Should().Be(log3Id);
		result[1].Id.Should().Be(log2Id);
		result[2].Id.Should().Be(log1Id);

		// JOIN с Workplace
		result[0].WorkplaceName.Should().Be("Упаковка");
		result[0].WorkplaceCode.Should().Be("PACKING");
		result[1].WorkplaceName.Should().Be("Торцовка");
		result[1].WorkplaceCode.Should().Be("CUTTING");

		// JOIN с Users: null для системного, имя для пользовательского
		result[0].UserId.Should().BeNull();
		result[0].UserName.Should().BeNull();
		result[2].UserId.Should().Be(userId);
		result[2].UserName.Should().Be("Мастер");

		// Остальные поля
		result[2].OperationType.Should().Be("START");
		result[2].Notes.Should().Be("Начало");
		result[2].Source.Should().Be("API");
	}

	[Fact]
	public async Task GetOrderOperationLogsAsync_ByIdentifier_ShouldResolveBothGuidAndOrderNumber()
	{
		// Arrange
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();
		var workplaceId = Guid.NewGuid();
		var t = DateTime.UtcNow;

		_context.Workplaces.Add(new Workplace { Id = workplaceId, Name = "Сборка", Code = "ASSEMBLY", IsWorkplace = true });
		_context.Orders.Add(new Order { Id = orderId, OrderNumber = "OP-123" });
		_context.ProductionOrders.Add(new ProductionOrder { Id = productionOrderId, OrderId = orderId });
		_context.OperationLogs.Add(new OperationLog
		{
			Id = Guid.NewGuid(),
			ProductionOrderId = productionOrderId,
			WorkplaceId = workplaceId,
			OperationType = "START",
			OperationTime = t,
			CreatedAt = t
		});
		await _context.SaveChangesAsync();

		// Act
		var byGuid = await _service.GetOrderOperationLogsAsync(orderId.ToString());
		var byNumber = await _service.GetOrderOperationLogsAsync("OP-123");

		// Assert
		byGuid.Should().NotBeNull();
		byGuid!.Should().HaveCount(1);
		byGuid[0].WorkplaceName.Should().Be("Сборка");

		byNumber.Should().NotBeNull();
		byNumber!.Should().HaveCount(1);
		byNumber[0].Id.Should().Be(byGuid[0].Id);
	}

	[Fact]
	public async Task GetOrderOperationLogsAsync_WithoutProductionOrder_ShouldReturnEmptyList()
	{
		// Arrange
		var orderId = Guid.NewGuid();
		_context.Orders.Add(new Order { Id = orderId, OrderNumber = "NO-PROD" });
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetOrderOperationLogsAsync("NO-PROD");

		// Assert
		result.Should().NotBeNull();     // не null — заказ есть
		result!.Should().BeEmpty();       // но логов нет
	}

	[Fact]
	public async Task GetOrderOperationLogsAsync_WhenOrderNotFound_ShouldReturnNull()
	{
		// Act
		var result = await _service.GetOrderOperationLogsAsync("NON-EXISTENT");
		var resultByGuid = await _service.GetOrderOperationLogsAsync(Guid.NewGuid().ToString());

		// Assert
		result.Should().BeNull();
		resultByGuid.Should().BeNull();
	}
}