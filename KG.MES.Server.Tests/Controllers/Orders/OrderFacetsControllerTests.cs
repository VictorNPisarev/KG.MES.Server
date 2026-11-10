using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Server.Tests.Helpers;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KG.MES.Shared.Tests.Controllers.Orders;

[Trait("Category", "Orders")]
public class OrderFacetsControllerTests : TestBase
{

	public OrderFacetsControllerTests(WebApplicationFactory<Program> factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetFacets_ShouldReturnDistinctValuesWithCounts()
	{
		// Arrange
		var client = CreateClient();

		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();
		var order3Id = Guid.NewGuid();
		var workplaceId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Сборка"; w.IsWorkplace = true; })
			.WithOrder(o =>
			{
				o.Id = order1Id;
				o.OrderNumber = "1001";
				o.WindowCount = 5;
				o.WindowArea = 10.5m;
				o.PlateCount = 2;
				o.PlateArea = 3.25m;
				o.IsEconom = true; 
			})
			.WithProductionOrder(po => { po.OrderId = order1Id; po.CurrentWorkplaceId = workplaceId; po.Machine = "Conturex"; })
			.WithOrder(o =>
			{
				o.Id = order2Id;
				o.OrderNumber = "1002";
				o.WindowCount = 4;
				o.WindowArea = 8.5m;
				o.PlateCount = 1;
				o.PlateArea = 1.25m;
				o.IsEconom = false;
			})
			.WithProductionOrder(po => { po.OrderId = order2Id; po.CurrentWorkplaceId = workplaceId; po.Machine = "Conturex"; })
			.WithOrder(o =>
			{
				o.Id = order3Id;
				o.OrderNumber = "1003";
				o.WindowCount = 3;
				o.WindowArea = 6m;
				o.PlateCount = 0;
				o.PlateArea = 0m;
				o.IsEconom = true;
			})
			.WithProductionOrder(po => { po.OrderId = order3Id; po.CurrentWorkplaceId = workplaceId; po.Machine = "Угловой центр"; }));

		var request = new FilterFacetsRequestDto
		{
			Fields = new List<string> { "IsEconom", "Machine" }
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/orders/facets", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<FilterFacetsResponseDto>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Facets.Should().ContainKey("IsEconom");
		result.Facets.Should().ContainKey("Machine");

		// IsEconom: Да (2), Нет (1)
		var economValues = result.Facets["IsEconom"];
		economValues.Should().HaveCount(2);

		// Machine: Conturex (2), Угловой центр (1)
		var machineValues = result.Facets["Machine"];
		machineValues.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetFacets_WithUnknownField_ShouldReturnEmptyList()
	{
		// Arrange
		var client = CreateClient();

		var request = new FilterFacetsRequestDto
		{
			Fields = new List<string> { "NonExistentField" }
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/orders/facets", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<FilterFacetsResponseDto>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result!.Facets.Should().ContainKey("NonExistentField");
		result.Facets["NonExistentField"].Should().BeEmpty();
	}

	[Fact]
	public async Task GetFacets_WithEmptyFields_ShouldReturnBadRequest()
	{
		// Arrange
		var client = CreateClient();

		var request = new FilterFacetsRequestDto { Fields = new List<string>() };

		// Act
		var response = await client.PostAsJsonAsync("/api/orders/facets", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}
}