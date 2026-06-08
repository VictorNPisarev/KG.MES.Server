using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Tests.Helpers;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace KG.MES.Server.Tests.Controllers.Orders;

[Trait("Category", "Orders")]
public class OrderLifecycleControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public OrderLifecycleControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task CreateOrder_ShouldCreateOrderProductionAndSupplyItems()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Lifecycle_Create");
		var client = customFactory.CreateClient();

		var noneId = Guid.NewGuid();
		var supplyTypeId1 = Guid.NewGuid();
		var supplyTypeId2 = Guid.NewGuid();

		// Подготовка БД: рабочее место "none" и активные типы снабжения
		new TestDataBuilder()
			.WithWorkplace(w => { w.Id = noneId; w.Name = "none"; w.IsWorkplace = false; })
			.WithSupplyType(st => { st.Id = supplyTypeId1; st.Name = "Фурнитура"; st.IsActive = true; })
			.WithSupplyType(st => { st.Id = supplyTypeId2; st.Name = "Стекло"; st.IsActive = true; })
			.Build(customFactory.Services);

		var request = new CreateOrderRequestDto
		{
			OrderNumber = "TEST-999",
			WindowCount = 2,
			WindowArea = 4.5m,
			PlateCount = 0,
			PlateArea = 0m,
			IsEconom = false,
			IsClaim = false,
			IsOnlyPaid = false,
			Comment = "Тестовый комментарий"
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/orders", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		// Используем dynamic или JsonElement, так как точный тип ответа может быть анонимным или специальным DTO
		var json = JsonDocument.Parse(content).RootElement;

		json.GetProperty("orderId").GetString().Should().NotBeEmpty();
		json.GetProperty("productionOrderId").GetString().Should().NotBeEmpty();
		json.GetProperty("orderSupplyId").GetString().Should().NotBeEmpty();

		var supplyItemIds = json.GetProperty("supplyItemIds");
		supplyItemIds.GetArrayLength().Should().Be(2); // По одному на каждый активный SupplyType

		// Дополнительная проверка в БД: ProductionOrder должен указывать на "none"
		var productionOrderId = Guid.Parse(json.GetProperty("productionOrderId").GetString()!);
		using var scope = customFactory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var prodOrder = await db.ProductionOrders.FindAsync(productionOrderId);
		prodOrder.Should().NotBeNull();
		prodOrder!.CurrentWorkplaceId.Should().Be(noneId);
	}

	[Fact]
	public async Task BeginOrderWorkplace_WhenNoFootprints_ShouldBuildFullPathAndLog()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Lifecycle_Start");
		var client = customFactory.CreateClient();

		var noneId = Guid.NewGuid();
		var startWorkplaceId = Guid.NewGuid(); // "Торцовка"
		var nextWorkplaceId = Guid.NewGuid();  // "Профилирование"
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();
		var userId = Guid.NewGuid();

		new TestDataBuilder()
			.WithWorkplace(w => { w.Id = noneId; w.Name = "none"; w.IsWorkplace = false; })
			.WithWorkplace(w => { w.Id = startWorkplaceId; w.Name = "Торцовка"; w.IsWorkplace = true; })
			.WithWorkplace(w => { w.Id = nextWorkplaceId; w.Name = "Профилирование"; w.IsWorkplace = true; })
			.WithWorkplaceTransition(t => { t.FromWorkplaceId = noneId; t.ToWorkplaceId = startWorkplaceId; })
			.WithWorkplaceTransition(t => { t.FromWorkplaceId = startWorkplaceId; t.ToWorkplaceId = nextWorkplaceId; })
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "START-1"; })
			.WithProductionOrder(po =>
			{
				po.Id = productionOrderId;
				po.OrderId = orderId;
				po.CurrentWorkplaceId = noneId;
			})
			.Build(customFactory.Services);

		var request = new BeginWorkplaceRequestDto
		{
			ProductionOrderId = productionOrderId,
			WorkplaceId = startWorkplaceId,
			UserId = userId,
			Notes = "Начинаем работу",
			Source = "API"
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/orders/operations/start", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		using var scope = customFactory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		// 1. CurrentWorkplaceId обновился
		var prodOrder = await db.ProductionOrders.FindAsync(productionOrderId);
		prodOrder!.CurrentWorkplaceId.Should().Be(startWorkplaceId);

		// 2. Футпринты созданы для ВСЕХ рабочих мест
		var footprints = await db.OrderFootprints
			.Where(fp => fp.ProductionOrderId == productionOrderId)
			.ToListAsync();

		footprints.Should().HaveCount(2);

		var startFp = footprints.Single(f => f.WorkplaceId == startWorkplaceId);
		startFp.Status.Should().Be("active"); // Стартовое = active

		// 3. ИСПРАВЛЕНИЕ: Следующий участок = pending (не planned!)
		var nextFp = footprints.Single(f => f.WorkplaceId == nextWorkplaceId);
		nextFp.Status.Should().Be("pending"); // ← ИСПРАВЛЕНО!

		// 4. OperationLog создан
		var logs = await db.OperationLogs
			.Where(l => l.ProductionOrderId == productionOrderId && l.OperationType == "START")
			.ToListAsync();
		logs.Should().HaveCount(1);
		logs[0].UserId.Should().Be(userId);
	}

	[Fact]
	public async Task CompleteOrderWorkplace_ShouldUpdateStatusAndLog()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Lifecycle_Complete");
		var client = customFactory.CreateClient();

		var workplaceId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();
		var userId = Guid.NewGuid();

		new TestDataBuilder()
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Сборка"; w.IsWorkplace = true; })
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "COMP-1"; })
			.WithProductionOrder(po => { po.Id = productionOrderId; po.OrderId = orderId; po.CurrentWorkplaceId = workplaceId; })
			.WithOrderFootprint(fp =>
			{
				fp.ProductionOrderId = productionOrderId;
				fp.WorkplaceId = workplaceId;
				fp.Status = "active"; // Уже в работе
			})
			.Build(customFactory.Services);

		var request = new CompleteWorkplaceRequestDto
		{
			ProductionOrderId = productionOrderId,
			WorkplaceId = workplaceId,
			UserId = userId,
			Notes = "Работа завершена",
			Source = "API"
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/orders/operations/complete", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		using var scope = customFactory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		// 1. Статус футпринта изменился на completed
		var footprint = await db.OrderFootprints
			.FirstOrDefaultAsync(fp => fp.ProductionOrderId == productionOrderId && fp.WorkplaceId == workplaceId);

		footprint.Should().NotBeNull();
		footprint!.Status.Should().Be("completed");

		// 2. Создан OperationLog
		var logs = await db.OperationLogs
			.Where(l => l.ProductionOrderId == productionOrderId && l.OperationType == "COMPLETE")
			.ToListAsync();

		logs.Should().HaveCount(1);
		logs[0].UserId.Should().Be(userId);
	}

	[Fact]
	public async Task SetOrderFootprintStatus_WhenNoFootprints_ShouldBuildPathAndUpdate()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Lifecycle_SetStatus");
		var client = customFactory.CreateClient();

		var noneId = Guid.NewGuid();
		var workplaceId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();
		var userId = Guid.NewGuid();

		new TestDataBuilder()
			.WithWorkplace(w => { w.Id = noneId; w.Name = "none"; w.IsWorkplace = false; })
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Покраска"; w.IsWorkplace = true; })
			.WithWorkplaceTransition(t => { t.FromWorkplaceId = noneId; t.ToWorkplaceId = workplaceId; })
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "SET-1"; })
			.WithProductionOrder(po => { po.Id = productionOrderId; po.OrderId = orderId; })
			// ВАЖНО: Футпринтов нет
			.Build(customFactory.Services);

		var request = new SetFootprintStatusRequestDto
		{
			Status = "active",
			UserId = userId,
			Notes = "Ручное изменение"
		};

		// Act
		var response = await client.PutAsJsonAsync($"/api/orders/footprint/{productionOrderId}/workplace/{workplaceId}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		using var scope = customFactory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		// 1. Футпринт создан и статус установлен
		var footprint = await db.OrderFootprints
			.FirstOrDefaultAsync(fp => fp.ProductionOrderId == productionOrderId && fp.WorkplaceId == workplaceId);

		footprint.Should().NotBeNull();
		footprint!.Status.Should().Be("active");

		// 2. CurrentWorkplaceId обновлен
		var prodOrder = await db.ProductionOrders.FindAsync(productionOrderId);
		prodOrder!.CurrentWorkplaceId.Should().Be(workplaceId);

		// 3. Создан OperationLog с типом MANUAL_UPDATE
		var logs = await db.OperationLogs
			.Where(l => l.ProductionOrderId == productionOrderId && l.OperationType == "MANUAL_UPDATE")
			.ToListAsync();

		logs.Should().HaveCount(1);
		logs[0].Notes.Should().Contain("Статус изменён с NULL на active");
	}

	private WebApplicationFactory<Program> SetupTestFactory(string dbName = "TestDb")
	{
		return _factory.WithWebHostBuilder(builder =>
		{
			builder.ConfigureServices(services =>
			{
				services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
				services.RemoveAll<DbContextOptions<AppDbContext>>();
				services.AddDbContext<AppDbContext>(options =>
				{
					options.UseInMemoryDatabase(dbName);
					options.ConfigureWarnings(warnings =>
						warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
				});
			});
		});
	}
}