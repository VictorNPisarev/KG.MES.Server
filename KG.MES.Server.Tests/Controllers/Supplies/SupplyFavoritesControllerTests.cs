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

namespace KG.MES.Server.Tests.Controllers.Supply;

[Trait("Category", "Supply")]
public class SupplyFavoritesControllerTests : TestBase
{
	public SupplyFavoritesControllerTests(WebApplicationFactory<Program> factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetFavoriteSupplyOrders_ShouldReturnOnlyCurrentUserFavorites()
	{
		// Arrange
		var client = CreateClient();
		var userId = Guid.NewGuid();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

		var workplaceId = Guid.NewGuid();
		var supplyTypeId = Guid.NewGuid();
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();
		var order3Id = Guid.NewGuid();
		var orderSupplyId1 = Guid.NewGuid();
		var orderSupplyId2 = Guid.NewGuid();
		var orderSupplyId3 = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Сборка"; w.IsWorkplace = true; })
			.WithSupplyType(st => { st.Id = supplyTypeId; st.Name = "lumber"; st.IsActive = true; })
			.WithOrder(o => { o.Id = order1Id; o.OrderNumber = "SPFAV-INT-001"; })
			.WithProductionOrder(po => { po.OrderId = order1Id; po.CurrentWorkplaceId = workplaceId; })
			.WithOrderSupply(os => { os.Id = orderSupplyId1; os.OrderId = order1Id; })
			.WithSupplyItem(si => { si.OrderSupplyId = orderSupplyId1; si.SupplyTypeId = supplyTypeId; })
			.WithOrder(o => { o.Id = order2Id; o.OrderNumber = "SPFAV-INT-002"; })
			.WithProductionOrder(po => { po.OrderId = order2Id; po.CurrentWorkplaceId = workplaceId; })
			.WithOrderSupply(os => { os.Id = orderSupplyId2; os.OrderId = order2Id; })
			.WithSupplyItem(si => { si.OrderSupplyId = orderSupplyId2; si.SupplyTypeId = supplyTypeId; })
			.WithOrder(o => { o.Id = order3Id; o.OrderNumber = "SPFAV-INT-003"; })
			.WithProductionOrder(po => { po.OrderId = order3Id; po.CurrentWorkplaceId = workplaceId; })
			.WithOrderSupply(os => { os.Id = orderSupplyId3; os.OrderId = order3Id; })
			.WithSupplyItem(si => { si.OrderSupplyId = orderSupplyId3; si.SupplyTypeId = supplyTypeId; }));

		var add1 = await client.PostAsync($"/api/orders/{order1Id}/favorite", null);
		var add2 = await client.PostAsync($"/api/orders/{order2Id}/favorite", null);

		add1.StatusCode.Should().Be(HttpStatusCode.OK);
		add2.StatusCode.Should().Be(HttpStatusCode.OK);

		// Act
		var response = await client.GetAsync("/api/supplies/favorites");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<SupplyDto>>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Should().HaveCount(2);
		result.Should().Contain(s => s.Id == order1Id);
		result.Should().Contain(s => s.Id == order2Id);
		result.Should().NotContain(s => s.Id == order3Id);
		result.Should().OnlyContain(s => s.IsFavorite);
	}

	[Fact]
	public async Task GetFavoriteSupplyOrders_WhenNoFavorites_ShouldReturnEmptyList()
	{
		// Arrange
		var client = CreateClient();
		var userId = Guid.NewGuid();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

		// Act
		var response = await client.GetAsync("/api/supplies/favorites");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<SupplyDto>>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Should().BeEmpty();
	}
}