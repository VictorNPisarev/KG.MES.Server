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

// Тот же самый Trait! xUnit объединит эти тесты с OrdersControllerTests при фильтрации
[Trait("Category", "Orders")]
public class OrderDetailControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public OrderDetailControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetOrderById_ShouldReturnOrderDetails()
	{
		// 1. Arrange
		var customFactory = SetupTestFactory("TestDb_OrderDetail");
		var client = customFactory.CreateClient();

		var orderId = Guid.Parse("1a701ed6-859d-443b-b640-f3223a35d6e2");
		var productionOrderId = Guid.Parse("1cbe4ffb-2d7e-4e25-a4ca-cde099744a27");
		var workplaceId = Guid.Parse("b11feab3-b3bb-47fc-a1c3-9e087ed1fc2e");

		new TestDataBuilder()
			.WithWorkplace(w =>
			{
				w.Id = workplaceId;
				w.Name = "Отгружен";
				w.IsWorkplace = true;
			})
			.WithOrder(o =>
			{
				o.Id = orderId;
				o.OrderNumber = "1362";
				o.ReadyDate = DateTime.Parse("2025-06-26T21:00:00.000Z");
				o.WindowCount = 0;
				o.WindowArea = 0m;
				o.PlateCount = 0;
				o.PlateArea = 0m;
				o.IsEconom = false;
				o.IsClaim = false;
				o.IsOnlyPaid = false;
				o.CreatedAt = DateTime.Parse("2026-06-03T10:19:36.258Z");
			})
			.WithProductionOrder(po =>
			{
				po.Id = productionOrderId;
				po.OrderId = orderId;
				po.CurrentWorkplaceId = workplaceId;
				po.Comment = "";
				po.Lumber = "";
				po.GlazingBead = "";
				po.IsTwoSidePaint = false;
			})
			.Build(customFactory.Services);

		// 2. Act
		var response = await client.GetAsync($"/api/orders/{orderId}");

		// 3. Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderDetailDto>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Id.Should().Be(orderId);
		result.OrderNumber.Should().Be("1362");
		result.WindowArea.Should().Be(0.00m); // Проверяем как строку, как в DTO
		result.CurrentStatus.Should().Be("Отгружен");
		result.IsTwoSidePaint.Should().BeFalse();
	}

	[Fact]
	public async Task GetOrderById_WithNonExistentId_ShouldReturnNotFound()
	{
		var customFactory = SetupTestFactory("TestDb_OrderDetail_NotFound");
		var client = customFactory.CreateClient();
		var nonExistentOrderId = Guid.NewGuid();

		var response = await client.GetAsync($"/api/orders/{nonExistentOrderId}");

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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