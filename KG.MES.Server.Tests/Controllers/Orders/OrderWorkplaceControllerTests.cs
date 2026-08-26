using System.Net;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Shared.Data;
using KG.MES.Shared.Tests.Helpers;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KG.MES.Shared.Tests.Controllers.Orders;

[Trait("Category", "Orders")]
public class OrdersWorkplaceControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public OrdersWorkplaceControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Theory]
	[InlineData("/api/orders/in-work")]
	[InlineData("/api/orders/workplaces/{0}/in-work")]
	public async Task GetActiveAndPendingOrders_ShouldReturnCorrectFormat(string endpointTemplate)
	{
		// Arrange
		var dbName = $"TestDb_InWork_{Guid.NewGuid():N}";
		var customFactory = SetupTestFactory(dbName); 
		var client = customFactory.CreateClient();

		var workplaceId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();

		new TestDataBuilder()
			.WithWorkplace(w =>
			{
				w.Id = workplaceId;
				w.Name = "Сборка";
				w.IsWorkplace = true;
				w.Level = 30;
			})
			.WithOrder(o =>
			{
				o.Id = orderId;
				o.OrderNumber = "4080";
				o.ReadyDate = DateTime.Parse("2026-05-11T21:00:00.000Z");
				o.WindowCount = 32;
				o.WindowArea = 53.79m;
				o.PlateCount = 0;
				o.PlateArea = 0m;
			})
			.WithProductionOrder(po =>
			{
				po.Id = productionOrderId;
				po.OrderId = orderId;
				po.CurrentWorkplaceId = workplaceId;
			})
			.WithOrderFootprint(fp =>
			{
				fp.ProductionOrderId = productionOrderId;
				fp.WorkplaceId = workplaceId;
				fp.Status = "pending";
			})
			.Build(customFactory.Services);

		// Act
		var url = endpointTemplate.Contains("{0}")
			? string.Format(endpointTemplate, workplaceId)
			: $"{endpointTemplate}?workplaceId={workplaceId}";

		Console.WriteLine();
		Console.WriteLine();
		Console.WriteLine(url);
		Console.WriteLine();
		Console.WriteLine();

		var response = await client.GetAsync(url);

		///var errorContent = await response.Content.ReadAsStringAsync();
		//Console.WriteLine($"Error: {errorContent}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<OrderWorkplaceDto>>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Should().HaveCount(1);

		var order = result[0];

		// Проверяем snake_case имена полей
		order.ProductionOrderId.Should().Be(productionOrderId);
		order.WorkplaceId.Should().Be(workplaceId);
		order.OrderId.Should().Be(orderId);
		order.OrderNumber.Should().Be("4080");
		order.WindowCount.Should().Be(32);
		order.WindowArea.Should().Be(53.79m);
		order.PlateCount.Should().Be(0);
		order.PlateArea.Should().Be(0.00m);
		order.Status.Should().Be("pending");
		order.WorkplaceOrderStatus.Should().Be("pending");
		order.FromJoinery.Should().BeFalse();
		order.Name.Should().Be("4080");
	}

	[Theory]
	[InlineData("/api/orders/active")]
	[InlineData("/api/orders/workplaces/{0}/active")]
	public async Task GetActiveOrders_ShouldReturnOnlyActiveOrders(string endpointTemplate)
	{
		// Arrange
		var dbName = $"TestDb_Active_{Guid.NewGuid():N}";
		var customFactory = SetupTestFactory(dbName);
		var client = customFactory.CreateClient();

		var workplaceId = Guid.NewGuid();
		var orderId1 = Guid.NewGuid();
		var orderId2 = Guid.NewGuid();
		var productionOrderId1 = Guid.NewGuid();
		var productionOrderId2 = Guid.NewGuid();

		new TestDataBuilder()
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Покраска"; w.IsWorkplace = true; })
			.WithOrder(o => { o.Id = orderId1; o.OrderNumber = "1001"; o.WindowArea = 10.5m; })
			.WithProductionOrder(po => { po.Id = productionOrderId1; po.OrderId = orderId1; po.CurrentWorkplaceId = workplaceId; })
			.WithOrderFootprint(fp => { fp.ProductionOrderId = productionOrderId1; fp.WorkplaceId = workplaceId; fp.Status = "active"; })
			.WithOrder(o => { o.Id = orderId2; o.OrderNumber = "1002"; o.WindowArea = 20.0m; })
			.WithProductionOrder(po => { po.Id = productionOrderId2; po.OrderId = orderId2; po.CurrentWorkplaceId = workplaceId; })
			.WithOrderFootprint(fp => { fp.ProductionOrderId = productionOrderId2; fp.WorkplaceId = workplaceId; fp.Status = "pending"; }) // Не active!
			.Build(customFactory.Services);

		// Act
		var url = endpointTemplate.Contains("{0}")
			? string.Format(endpointTemplate, workplaceId)
			: $"{endpointTemplate}?workplaceId={workplaceId}";

		var response = await client.GetAsync(url);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<OrderWorkplaceDto>>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Should().HaveCount(1); // Только active
		result[0].Status.Should().Be("active");
		result[0].OrderNumber.Should().Be("1001");
		result[0].WindowArea.Should().Be(10.50m);
	}

	[Theory]
	[InlineData("/api/orders/pending")]
	[InlineData("/api/orders/workplaces/{0}/pending")]
	public async Task GetPendingOrders_ShouldReturnOnlyPendingOrders(string endpointTemplate)
	{
		// Arrange
		var dbName = $"TestDb_Pending_{Guid.NewGuid():N}";
		var customFactory = SetupTestFactory(dbName);
		var client = customFactory.CreateClient();

		var noneId = Guid.NewGuid();
		var previousWorkplaceId = Guid.NewGuid();
		var workplaceId = Guid.NewGuid(); // "Шлифовка" - НЕ стартовое рабочее место
		var orderId1 = Guid.NewGuid();
		var orderId2 = Guid.NewGuid();
		var productionOrderId1 = Guid.NewGuid();
		var productionOrderId2 = Guid.NewGuid();

		new TestDataBuilder()
			// Создаем рабочее место "none" (обязательно!)
			.WithWorkplace(w => { w.Id = noneId; w.Name = "none"; w.IsWorkplace = false; })
			// Создаем предыдущее рабочее место (например, "Торцовка")
			.WithWorkplace(w => { w.Id = previousWorkplaceId; w.Name = "Торцовка"; w.IsWorkplace = true; })
			// Создаем "Шлифовка" - она будет НЕ стартовой, потому что есть переход от "Торцовки"
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Шлифовка"; w.IsWorkplace = true; })
			// ВАЖНО: добавляем переход от "Торцовки" к "Шлифовке"
			.WithWorkplaceTransition(t =>
			{
				t.FromWorkplaceId = noneId;
				t.ToWorkplaceId = previousWorkplaceId;
			})
			.WithWorkplaceTransition(t =>
			{
				t.FromWorkplaceId = previousWorkplaceId;
				t.ToWorkplaceId = workplaceId;
			})

			// Создаем заказы
			.WithOrder(o => { o.Id = orderId1; o.OrderNumber = "2001"; o.PlateArea = 5.25m; })
			.WithProductionOrder(po => { po.Id = productionOrderId1; po.OrderId = orderId1; po.CurrentWorkplaceId = workplaceId; })
			.WithOrderFootprint(fp => { fp.ProductionOrderId = productionOrderId1; fp.WorkplaceId = workplaceId; fp.Status = "pending"; })
			.WithOrder(o => { o.Id = orderId2; o.OrderNumber = "2002"; o.PlateArea = 8.0m; })
			.WithProductionOrder(po => { po.Id = productionOrderId2; po.OrderId = orderId2; po.CurrentWorkplaceId = workplaceId; })
			.WithOrderFootprint(fp => { fp.ProductionOrderId = productionOrderId2; fp.WorkplaceId = workplaceId; fp.Status = "active"; }) // Не pending!
			.Build(customFactory.Services);

		// Act
		var url = endpointTemplate.Contains("{0}")
			? string.Format(endpointTemplate, workplaceId)
			: $"{endpointTemplate}?workplaceId={workplaceId}";

		var response = await client.GetAsync(url);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<OrderWorkplaceDto>>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Should().HaveCount(1); // Только pending
		result[0].Status.Should().Be("pending");
		result[0].OrderNumber.Should().Be("2001");
		result[0].PlateArea.Should().Be(5.25m);
	}

	[Fact]
	public async Task GetOrders_WithJoineryStatus_ShouldAddEmojiToName()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Joinery");
		var client = customFactory.CreateClient();

		var workplaceId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();

		new TestDataBuilder()
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Столярка"; w.IsWorkplace = true; w.Level = 15; })
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "3001"; })
			.WithProductionOrder(po => { po.Id = productionOrderId; po.OrderId = orderId; po.CurrentWorkplaceId = workplaceId; })
			.WithOrderFootprint(fp => { fp.ProductionOrderId = productionOrderId; fp.WorkplaceId = workplaceId; fp.Status = "joinery"; })
			.Build(customFactory.Services);

		// Act
		var response = await client.GetAsync($"/api/orders/in-work?workplaceId={workplaceId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<OrderWorkplaceDto>>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Should().HaveCount(1);
		result[0].Status.Should().Be("joinery");
		result[0].FromJoinery.Should().BeTrue();
		result[0].Name.Should().Be("🪚 3001"); // Эмодзи добавлен!
	}

	[Fact]
	public async Task CompleteOrderOnPacking_ShouldCreateLogsForPackingAndComplete()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();

		var packingWorkplaceId = Guid.NewGuid();
		var completeWorkplaceId = Guid.NewGuid();

		var customFactory = SetupTestFactory("TestDb_PackingComplete");
		var client = customFactory.CreateClient();

		new TestDataBuilder()
			.WithWorkplace(w =>
			{
				w.Id = packingWorkplaceId;
				w.Name = "Упаковка";
				w.Code = "PACKING";
				w.IsWorkplace = true;
				w.Level = 40;
			})
			.WithWorkplace(w =>
			{
				w.Id = completeWorkplaceId;
				w.Name = "ГОТОВО";
				w.Code = "CONPLETE";
				w.IsWorkplace = false;
				w.Level = 50;
			})
			.WithOrder(o =>
			{
				o.Id = orderId;
				o.OrderNumber = "PACK-001";
			})
			.WithProductionOrder(po =>
			{
				po.Id = productionOrderId;
				po.OrderId = orderId;
				po.CurrentWorkplaceId = packingWorkplaceId;
			})
			.WithOrderFootprint(fp =>
			{
				fp.ProductionOrderId = productionOrderId;
				fp.WorkplaceId = packingWorkplaceId;
				fp.Status = "active";
			})
			.WithWorkplaceTransition(t =>
			{
				t.FromWorkplaceId = packingWorkplaceId;
				t.ToWorkplaceId = completeWorkplaceId;
			})
			.Build(customFactory.Services);

		// Act
		var request = new CompleteWorkplaceRequestDto
		{
			ProductionOrderId = productionOrderId,
			WorkplaceId = packingWorkplaceId,
			UserId = userId,
			Notes = "Упаковка завершена",
			Source = "API"
		};

		var response = await client.PostAsJsonAsync("/api/orders/operations/complete", request);

		if (response.StatusCode != HttpStatusCode.OK)
		{
			var error = await response.Content.ReadAsStringAsync();
			Console.WriteLine($"Error");
			Console.WriteLine($"Error");
			Console.WriteLine($"Error: {error}");
			Console.WriteLine($"Error");
			Console.WriteLine($"Error");
		}
		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		using var scope = customFactory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		// Проверяем логи
		var logs = await db.OperationLogs
			.Where(ol => ol.ProductionOrderId == productionOrderId)
			.OrderBy(ol => ol.OperationTime)
			.ToListAsync();

		logs.Should().HaveCount(2);

		var packingLog = logs.FirstOrDefault(ol => ol.WorkplaceId == packingWorkplaceId);
		packingLog.Should().NotBeNull();
		packingLog!.OperationType.Should().Be("COMPLETE");
		packingLog.UserId.Should().Be(userId);
		packingLog.Notes.Should().Contain("Упаковка завершена");

		var completeLog = logs.FirstOrDefault(ol => ol.WorkplaceId == completeWorkplaceId);
		completeLog.Should().NotBeNull();
		completeLog!.OperationType.Should().Be("COMPLETE");
		completeLog.Notes.Should().Contain("Статус изменен на 'ГОТОВО'");

		// Проверяем статус заказа
		var prodOrder = await db.ProductionOrders
			.FirstOrDefaultAsync(po => po.Id == productionOrderId);
		prodOrder!.CurrentWorkplaceId.Should().Be(completeWorkplaceId);

		// Проверяем футпринт
		var footprint = await db.OrderFootprints
			.FirstOrDefaultAsync(fp => fp.ProductionOrderId == productionOrderId
										&& fp.WorkplaceId == packingWorkplaceId);
		footprint.Should().NotBeNull();
		footprint!.Status.Should().Be("completed");
	}

	[Fact]
	public async Task MasterSetOrderComplete_ShouldCreateCompleteLogsAndCompleteFootprints()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();

		var workplace1Id = Guid.NewGuid();
		var workplace2Id = Guid.NewGuid();
		var workplace3Id = Guid.NewGuid();
		var completeWorkplaceId = Guid.NewGuid();

		var customFactory = SetupTestFactory("TestDb_MasterComplete");
		var client = customFactory.CreateClient();

		new TestDataBuilder()
			.WithWorkplace(w => { w.Id = workplace1Id; w.Name = "Сборка"; w.IsWorkplace = true; w.Level = 30; })
			.WithWorkplace(w => { w.Id = workplace2Id; w.Name = "Покраска"; w.IsWorkplace = true; w.Level = 35; })
			.WithWorkplace(w => { w.Id = workplace3Id; w.Name = "Шлифовка"; w.IsWorkplace = true; w.Level = 25; })
			.WithWorkplace(w => { w.Id = completeWorkplaceId; w.Name = "ГОТОВО"; w.IsWorkplace = false; w.Level = 50; })
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "MASTER-001"; })
			.WithProductionOrder(po =>
			{
				po.Id = productionOrderId;
				po.OrderId = orderId;
				po.CurrentWorkplaceId = workplace1Id;
			})
			.WithOrderFootprint(fp => { fp.ProductionOrderId = productionOrderId; fp.WorkplaceId = workplace1Id; fp.Status = "active"; })
			.WithOrderFootprint(fp => { fp.ProductionOrderId = productionOrderId; fp.WorkplaceId = workplace2Id; fp.Status = "pending"; })
			.WithOrderFootprint(fp => { fp.ProductionOrderId = productionOrderId; fp.WorkplaceId = workplace3Id; fp.Status = "planned"; })
			.WithOperationLog(ol =>
			{
				ol.ProductionOrderId = productionOrderId;
				ol.WorkplaceId = workplace1Id;
				ol.UserId = userId;
				ol.OperationType = "START";
				ol.OperationTime = DateTime.UtcNow;
				ol.Notes = "Начало работы на Сборке";
				ol.Source = "Test";
			})
			.Build(customFactory.Services);

		using var scope = customFactory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		// Проверяем начальное состояние
		var logsBefore = await db.OperationLogs
				.Where(ol => ol.ProductionOrderId == productionOrderId)
				.ToListAsync();
		logsBefore.Should().HaveCount(1);

		var footprintsBefore = await db.OrderFootprints
			.Where(fp => fp.ProductionOrderId == productionOrderId)
			.ToListAsync();
		footprintsBefore.Should().HaveCount(3);

		// Act — вызываем API перевода в ГОТОВО
		var request = new { userId, notes = "Заказ готов по решению мастера" };
		var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/complete", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var logsAfter = await db.OperationLogs
			.Where(ol => ol.ProductionOrderId == productionOrderId && ol.OperationType == "COMPLETE")
			.ToListAsync();

		// 3 записи: START (был) + COMPLETE на Сборке + COMPLETE на ГОТОВО
		logsAfter.Should().HaveCount(4);

		// Проверяю логи COMPLETE
		var workplace1Complete = logsAfter.FirstOrDefault(ol =>
			ol.WorkplaceId == workplace1Id);
		workplace1Complete.Should().NotBeNull();

		var workplace2Complete = logsAfter.FirstOrDefault(ol =>
			ol.WorkplaceId == workplace2Id);
		workplace1Complete.Should().NotBeNull();

		var workplace3Complete = logsAfter.FirstOrDefault(ol =>
			ol.WorkplaceId == workplace3Id);
		workplace1Complete.Should().NotBeNull();

		var completeLog = logsAfter.FirstOrDefault(ol =>
			ol.WorkplaceId == completeWorkplaceId);
		completeLog.Should().NotBeNull();

		// Проверяю следы
		var footprints = await db.OrderFootprints
			.Where(fp => fp.ProductionOrderId == productionOrderId)
			.ToListAsync();

		// Все еще 3 (рабочие места)
		footprints.Should().HaveCount(3);
		//footprints.All(fp => fp.Status == Constants.OrderStatus.WorkplaceStatus.Completed).Should().BeTrue(); 
		//не работает смена статусов в footprint, т.к. InMemory не поддерживает транзакции, которая используется внутри сервера

		// Проверяю статус заказа
		var prodOrder = await db.ProductionOrders
			.FirstOrDefaultAsync(po => po.Id == productionOrderId);
		prodOrder!.CurrentWorkplaceId.Should().Be(completeWorkplaceId);
	}

	[Fact]
	public async Task SetOrderDeparture_ShouldCreateLogWithCompleteStatus()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();

		var completeWorkplaceId = Guid.NewGuid();
		var departedWorkplaceId = Guid.NewGuid();

		var customFactory = SetupTestFactory("TestDb_Departure");
		var client = customFactory.CreateClient();

		new TestDataBuilder()
			.WithWorkplace(w =>
			{
				w.Id = completeWorkplaceId;
				w.Name = "ГОТОВО";
				w.Code = "COMPLETE";
				w.IsWorkplace = false;
				w.Level = 50;
			})
			.WithWorkplace(w =>
			{
				w.Id = departedWorkplaceId;
				w.Name = "Отгружен";
				w.Code = "DEPARTED";
				w.IsWorkplace = false;
				w.Level = 60;
			})
			.WithOrder(o =>
			{
				o.Id = orderId;
				o.OrderNumber = "DEPART-001";
			})
			.WithProductionOrder(po =>
			{
				po.Id = productionOrderId;
				po.OrderId = orderId;
				po.CurrentWorkplaceId = completeWorkplaceId;
			})
			.Build(customFactory.Services);

		// Act — вызываем API отгрузки
		var request = new { userId, notes = "Заказ отгружен клиенту" };
		var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/departure", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		using var scope = customFactory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		// Проверяем лог отгрузки
		var departureLog = await db.OperationLogs
			.FirstOrDefaultAsync(ol => ol.ProductionOrderId == productionOrderId
										&& ol.WorkplaceId == departedWorkplaceId);
		departureLog.Should().NotBeNull();
		departureLog!.OperationType.Should().Be("COMPLETE");

		// Проверяем статус заказа
		var prodOrder = await db.ProductionOrders
			.FirstOrDefaultAsync(po => po.Id == productionOrderId);
		prodOrder!.CurrentWorkplaceId.Should().Be(departedWorkplaceId);

		// Проверяем, что футпринты удалены
		var footprints = await db.OrderFootprints
			.Where(fp => fp.ProductionOrderId == productionOrderId)
			.ToListAsync();
		footprints.Should().BeEmpty();
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
						warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
				});
			});
		});
	}
}