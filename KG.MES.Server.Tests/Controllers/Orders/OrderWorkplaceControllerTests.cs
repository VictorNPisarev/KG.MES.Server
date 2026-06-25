using System.Net;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Tests.Helpers;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace KG.MES.Server.Tests.Controllers.Orders;

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
		var customFactory = SetupTestFactory("TestDb_InWork");
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
		var customFactory = SetupTestFactory("TestDb_Active");
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
		var customFactory = SetupTestFactory("TestDb_Pending");
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

	private WebApplicationFactory<Program> SetupTestFactory(string dbName = "TestDb")
	{
		return _factory.WithWebHostBuilder(builder =>
		{
			builder.ConfigureServices(services =>
			{
				services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
				services.RemoveAll<DbContextOptions<AppDbContext>>();
				services.AddDbContext<AppDbContext>(options =>
					options.UseInMemoryDatabase(dbName));
			});
		});
	}
}