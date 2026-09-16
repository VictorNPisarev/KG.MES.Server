using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Shared.Data;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace KG.MES.Shared.Tests.Controllers.Orders;

[Trait("Category", "Orders")]
public class OrderFacetsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public OrderFacetsControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetFacets_ShouldReturnDistinctValuesWithCounts()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Facets");
		var client = customFactory.CreateClient();

		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();
		var order3Id = Guid.NewGuid();
		var workplaceId = Guid.NewGuid();

		new TestDataBuilder()
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
			.WithProductionOrder(po => { po.OrderId = order3Id; po.CurrentWorkplaceId = workplaceId; po.Machine = "Угловой центр"; })
			.Build(customFactory.Services);

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
		var customFactory = SetupTestFactory("TestDb_Facets_Unknown");
		var client = customFactory.CreateClient();

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
		var customFactory = SetupTestFactory("TestDb_Facets_Empty");
		var client = customFactory.CreateClient();

		var request = new FilterFacetsRequestDto { Fields = new List<string>() };

		// Act
		var response = await client.PostAsJsonAsync("/api/orders/facets", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	private WebApplicationFactory<Program> SetupTestFactory(string dbName)
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