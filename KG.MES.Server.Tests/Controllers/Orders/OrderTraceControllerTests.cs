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
public class OrderTraceControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public OrderTraceControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetOrderTrace_WhenIdentifierIsGuid_ShouldReturnTrace()
	{
		// 1. Arrange
		var customFactory = SetupTestFactory("TestDb_OrderTrace_Guid");
		var client = customFactory.CreateClient();

		var orderId = Guid.Parse("a8d03ea4-3522-4dbb-999c-804bb7035dff");
		var productionOrderId = Guid.Parse("6dad36f6-c235-475c-9b8b-4ef9854ea497");

		var workplace1Id = Guid.Parse("097adc1a-9144-419f-b264-3bf5dc0623d3");
		var workplace2Id = Guid.Parse("cf806c33-98d0-4852-9923-e9b00c31581c");
		var workplace3Id = Guid.Parse("018953eb-647d-4044-8bfa-1fc419e69952");

		new TestDataBuilder()
			.WithOrder(o =>
			{
				o.Id = orderId;
				o.OrderNumber = "1014";
				o.ReadyDate = DateTime.Parse("2026-06-21T21:00:00.000Z");
			})
			.WithProductionOrder(po =>
			{
				po.Id = productionOrderId;
				po.OrderId = orderId;
			})
			.WithWorkplace(w => { w.Id = workplace1Id; w.Name = "Торцовка"; })
			.WithWorkplace(w => { w.Id = workplace2Id; w.Name = "Профилирование"; })
			.WithWorkplace(w => { w.Id = workplace3Id; w.Name = "Сборка"; })
			.WithOrderFootprint(fp =>
			{
				fp.ProductionOrderId = productionOrderId;
				fp.WorkplaceId = workplace1Id;
				fp.Status = "completed";
			})
			.WithOrderFootprint(fp =>
			{
				fp.ProductionOrderId = productionOrderId;
				fp.WorkplaceId = workplace2Id;
				fp.Status = "pending";
			})
			.WithOrderFootprint(fp =>
			{
				fp.ProductionOrderId = productionOrderId;
				fp.WorkplaceId = workplace3Id;
				fp.Status = "planned";
			})
			.Build(customFactory.Services);

		// 2. Act (передаем GUID)
		var response = await client.GetAsync($"/api/orders/{orderId}/trace");

		// 3. Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderTraceResponse>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Orders.Should().HaveCount(1);

		var trace = result.Orders[0];
		trace.OrderId.Should().Be(orderId);
		trace.ProductionOrderId.Should().Be(productionOrderId);
		trace.OrderNumber.Should().Be("1014");
		trace.ReadyDate.Should().Be(DateTime.Parse("2026-06-21T21:00:00.000Z"));

		trace.Workplaces.Should().HaveCount(3);

		trace.Workplaces[0].WorkplaceId.Should().Be(workplace1Id);
		trace.Workplaces[0].WorkplaceName.Should().Be("Торцовка");
		trace.Workplaces[0].Status.Should().Be("completed");

		trace.Workplaces[1].WorkplaceId.Should().Be(workplace2Id);
		trace.Workplaces[1].WorkplaceName.Should().Be("Профилирование");
		trace.Workplaces[1].Status.Should().Be("pending");

		trace.Workplaces[2].WorkplaceId.Should().Be(workplace3Id);
		trace.Workplaces[2].WorkplaceName.Should().Be("Сборка");
		trace.Workplaces[2].Status.Should().Be("planned");
	}

	[Fact]
	public async Task GetOrderTrace_WhenIdentifierIsOrderNumber_ShouldReturnTrace()
	{
		// 1. Arrange
		var customFactory = SetupTestFactory("TestDb_OrderTrace_Number");
		var client = customFactory.CreateClient();

		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();
		var workplaceId = Guid.NewGuid();

		new TestDataBuilder()
			.WithOrder(o =>
			{
				o.Id = orderId;
				o.OrderNumber = "2025";
				o.ReadyDate = DateTime.UtcNow.AddDays(30);
			})
			.WithProductionOrder(po =>
			{
				po.Id = productionOrderId;
				po.OrderId = orderId;
			})
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Покраска"; })
			.WithOrderFootprint(fp =>
			{
				fp.ProductionOrderId = productionOrderId;
				fp.WorkplaceId = workplaceId;
				fp.Status = "pending";
			})
			.Build(customFactory.Services);

		// 2. Act (передаем номер заказа, а не GUID)
		var response = await client.GetAsync("/api/orders/2025/trace");

		// 3. Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderTraceResponse>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Orders.Should().HaveCount(1);

		var trace = result.Orders[0];
		trace.OrderId.Should().Be(orderId);
		trace.OrderNumber.Should().Be("2025");
		trace.Workplaces.Should().HaveCount(1);
		trace.Workplaces[0].WorkplaceName.Should().Be("Покраска");
		trace.Workplaces[0].Status.Should().Be("pending");
	}

	[Fact]
	public async Task GetOrderTrace_WhenOrderNotFound_ShouldReturnNotFound()
	{
		// 1. Arrange
		var customFactory = SetupTestFactory("TestDb_OrderTrace_NotFound");
		var client = customFactory.CreateClient();

		var nonExistentOrderId = Guid.NewGuid();

		// 2. Act
		var response = await client.GetAsync($"/api/orders/{nonExistentOrderId}/trace");

		// 3. Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
	
	[Fact]
	public async Task GetOrderTrace_WhenOrderHasNoProductionOrder_ShouldReturnTraceWithEmptyWorkplaces()
	{
		// 1. Arrange
		var customFactory = SetupTestFactory("TestDb_OrderTrace_NoProduction");
		var client = customFactory.CreateClient();

		var orderId = Guid.NewGuid();

		new TestDataBuilder()
			.WithOrder(o =>
			{
				o.Id = orderId;
				o.OrderNumber = "3030";
				o.ReadyDate = DateTime.UtcNow.AddDays(15);
			})
			// НЕ создаем ProductionOrder
			.Build(customFactory.Services);

		// 2. Act
		var response = await client.GetAsync($"/api/orders/{orderId}/trace");

		// 3. Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderTraceResponse>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Orders.Should().HaveCount(1);

		var trace = result.Orders[0];
		trace.OrderId.Should().Be(orderId);
		trace.OrderNumber.Should().Be("3030");
		trace.ProductionOrderId.Should().BeNull();
		trace.Workplaces.Should().BeEmpty();
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

// DTO для десериализации ответа (обертка вокруг массива)
public class OrderTraceResponse
{
	public List<OrderTraceDto> Orders { get; set; } = [];
}