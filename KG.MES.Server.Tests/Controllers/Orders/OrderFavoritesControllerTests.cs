using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using KG.MES.Server.Tests.Helpers;
using KG.MES.Shared.Serialization;

namespace KG.MES.Server.Tests.Controllers.Orders;

[Trait("Category", "Orders")]
public class OrderFavoritesControllerTests : TestBase
{
	public OrderFavoritesControllerTests(WebApplicationFactory<Program> factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetFavorites_ShouldReturnOnlyCurrentUserFavorites()
	{
		// Arrange
		var client = CreateClient();

		var userId = Guid.NewGuid();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

		var workplaceId = Guid.NewGuid();
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();
		var order3Id = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Сборка"; w.IsWorkplace = true; })
			.WithOrder(o => { o.Id = order1Id; o.OrderNumber = "FAV-INT-001"; })
			.WithProductionOrder(po => { po.OrderId = order1Id; po.CurrentWorkplaceId = workplaceId; })
			.WithOrder(o => { o.Id = order2Id; o.OrderNumber = "FAV-INT-002"; })
			.WithProductionOrder(po => { po.OrderId = order2Id; po.CurrentWorkplaceId = workplaceId; })
			.WithOrder(o => { o.Id = order3Id; o.OrderNumber = "FAV-INT-003"; })
			.WithProductionOrder(po => { po.OrderId = order3Id; po.CurrentWorkplaceId = workplaceId; }));

		// TestAuthHandler даёт случайный CurrentUserId; нам нужно его узнать,
		// чтобы добавить закладки от имени этого пользователя.
		// Но проще: добавить через API POST (тогда userId = текущий из TestAuthHandler).
		var add1 = await client.PostAsync($"/api/orders/{order1Id}/favorite", null);
		var add2 = await client.PostAsync($"/api/orders/{order2Id}/favorite", null);

		add1.StatusCode.Should().Be(HttpStatusCode.OK);
		add2.StatusCode.Should().Be(HttpStatusCode.OK);

		// Act
		var response = await client.GetAsync("/api/orders/favorites");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<OrderDto>>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Should().HaveCount(2);
		result.Should().Contain(o => o.Id == order1Id);
		result.Should().Contain(o => o.Id == order2Id);
		result.Should().NotContain(o => o.Id == order3Id);
		result.Should().OnlyContain(o => o.IsFavorite);
	}

	[Fact]
	public async Task AddFavorite_ShouldBeIdempotent()
	{
		// Arrange
		var client = CreateClient();

		var userId = Guid.NewGuid();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
		
		var workplaceId = Guid.NewGuid();
		var orderId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Сборка"; w.IsWorkplace = true; })
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "FAV-INT-004"; })
			.WithProductionOrder(po => { po.OrderId = orderId; po.CurrentWorkplaceId = workplaceId; }));

		// Act — добавляем дважды
		var first = await client.PostAsync($"/api/orders/{orderId}/favorite", null);
		var second = await client.PostAsync($"/api/orders/{orderId}/favorite", null);

		// Assert
		first.StatusCode.Should().Be(HttpStatusCode.OK);
		second.StatusCode.Should().Be(HttpStatusCode.OK);

		// В БД — только одна запись
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var count = await db.OrderFavorites.CountAsync(f => f.OrderId == orderId);
		count.Should().Be(1);
	}

	[Fact]
	public async Task RemoveFavorite_ShouldDeleteFavorite()
	{
		// Arrange
		var client = CreateClient();

		var userId = Guid.NewGuid();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

		var workplaceId = Guid.NewGuid();
		var orderId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Сборка"; w.IsWorkplace = true; })
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "FAV-INT-005"; })
			.WithProductionOrder(po => { po.OrderId = orderId; po.CurrentWorkplaceId = workplaceId; }));

		// Добавляем
		await client.PostAsync($"/api/orders/{orderId}/favorite", null);

		// Act — удаляем
		var response = await client.DeleteAsync($"/api/orders/{orderId}/favorite");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var count = await db.OrderFavorites.CountAsync(f => f.OrderId == orderId);
		count.Should().Be(0);
	}

	[Fact]
	public async Task RemoveFavorite_WhenNotExists_ShouldBeIdempotent()
	{
		// Arrange
		var client = CreateClient();

		var orderId = Guid.NewGuid();
		BuildTestData(builder => builder
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "FAV-INT-006"; }));

		// Act — удаляем то, чего нет
		var response = await client.DeleteAsync($"/api/orders/{orderId}/favorite");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task AddFavorite_WhenOrderNotFound_ShouldReturnNotFound()
	{
		// Arrange
		var client = CreateClient();
		var nonExistentOrderId = Guid.NewGuid();

		// Act
		var response = await client.PostAsync($"/api/orders/{nonExistentOrderId}/favorite", null);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task GetOrders_ShouldIncludeIsFavoriteFlag()
	{
		// Arrange
		var client = CreateClient();

		var userId = Guid.NewGuid();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

		var workplaceId = Guid.NewGuid();
		var favOrderId = Guid.NewGuid();
		var normalOrderId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Сборка"; w.IsWorkplace = true; })
			.WithOrder(o => { o.Id = favOrderId; o.OrderNumber = "FAV-INT-101"; o.ReadyDate = DateTime.UtcNow.AddDays(1); })
			.WithProductionOrder(po => { po.OrderId = favOrderId; po.CurrentWorkplaceId = workplaceId; })
			.WithOrder(o => { o.Id = normalOrderId; o.OrderNumber = "FAV-INT-102"; o.ReadyDate = DateTime.UtcNow.AddDays(2); })
			.WithProductionOrder(po => { po.OrderId = normalOrderId; po.CurrentWorkplaceId = workplaceId; }));

		// Добавляем один заказ в избранное через API
		await client.PostAsync($"/api/orders/{favOrderId}/favorite", null);

		// Act — получаем общий список
		var response = await client.GetAsync("/api/orders?page=1&limit=50&sortBy=ReadyDate&sortOrder=asc");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<PaginatedResponse<OrderDto>>(content,
			new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				Converters = { new TotalsDtoConverter() }
			});
		result.Should().NotBeNull();
		result!.Data.Should().HaveCount(2);

		var favOrder = result.Data.First(o => o.Id == favOrderId);
		favOrder.IsFavorite.Should().BeTrue();

		var normalOrder = result.Data.First(o => o.Id == normalOrderId);
		normalOrder.IsFavorite.Should().BeFalse();
	}
}