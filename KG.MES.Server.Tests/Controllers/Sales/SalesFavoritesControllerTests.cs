using System.Net;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using KG.MES.Server.Tests.Helpers;

namespace KG.MES.Server.Tests.Controllers.Sales;

[Trait("Category", "Sales")]
public class SalesFavoritesControllerTests : TestBase
{
	public SalesFavoritesControllerTests(WebApplicationFactory<Program> factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetFavoriteSalesOrders_ShouldReturnOnlyCurrentUserFavorites()
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
			.WithOrder(o => { o.Id = order1Id; o.OrderNumber = "SFAV-INT-001"; })
			.WithProductionOrder(po => { po.OrderId = order1Id; po.CurrentWorkplaceId = workplaceId; })
			.WithOrder(o => { o.Id = order2Id; o.OrderNumber = "SFAV-INT-002"; })
			.WithProductionOrder(po => { po.OrderId = order2Id; po.CurrentWorkplaceId = workplaceId; })
			.WithOrder(o => { o.Id = order3Id; o.OrderNumber = "SFAV-INT-003"; })
			.WithProductionOrder(po => { po.OrderId = order3Id; po.CurrentWorkplaceId = workplaceId; }));

		// Добавляем два заказа в избранное через API
		var add1 = await client.PostAsync($"/api/orders/{order1Id}/favorite", null);
		var add2 = await client.PostAsync($"/api/orders/{order2Id}/favorite", null);

		add1.StatusCode.Should().Be(HttpStatusCode.OK);
		add2.StatusCode.Should().Be(HttpStatusCode.OK);

		// Act
		var response = await client.GetAsync("/api/sales/orders/favorites");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<SalesOrderDto>>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Should().HaveCount(2);
		result.Should().Contain(o => o.Id == order1Id);
		result.Should().Contain(o => o.Id == order2Id);
		result.Should().NotContain(o => o.Id == order3Id);
		result.Should().OnlyContain(o => o.IsFavorite);
	}

	[Fact]
	public async Task GetFavoriteSalesOrders_WhenNoFavorites_ShouldReturnEmptyList()
	{
		// Arrange
		var client = CreateClient();
		var userId = Guid.NewGuid();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

		// Act
		var response = await client.GetAsync("/api/sales/orders/favorites");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<SalesOrderDto>>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Should().BeEmpty();
	}
}