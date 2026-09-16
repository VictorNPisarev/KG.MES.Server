using System.Net;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Shared.Data;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Serialization;
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
public class OrderTotalsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public OrderTotalsControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	protected static readonly JsonSerializerOptions jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new TotalsDtoConverter() }
	};


	[Fact]
	public async Task GetOrders_ShouldReturnTotalsForAllRecords()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Totals");
		var client = customFactory.CreateClient();

		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();
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
			})
			.WithProductionOrder(po => { po.OrderId = order1Id; po.CurrentWorkplaceId = workplaceId; })
			.WithOrder(o =>
			{
				o.Id = order2Id;
				o.OrderNumber = "1002";
				o.WindowCount = 3;
				o.WindowArea = 6.75m;
				o.PlateCount = 1;
				o.PlateArea = 1.5m;
			})
			.WithProductionOrder(po => { po.OrderId = order2Id; po.CurrentWorkplaceId = workplaceId; })
			.Build(customFactory.Services);

		// Act
		var response = await client.GetAsync("/api/orders?page=1&limit=50");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		//var json = JsonDocument.Parse(content).RootElement;

		var result = JsonSerializer.Deserialize<PaginatedResponse<OrderDto>>(content, new JsonSerializerOptions
		{
			Converters = { new TotalsDtoConverter() },
			PropertyNameCaseInsensitive = true
		});

		// Проверяем totals
		result?.Totals.Should().NotBeNull();
		var totals = (OrderTotalsDto?)result?.Totals;
		totals?.WindowCountTotal.Should().Be(8);      // 5 + 3
		totals?.WindowAreaTotal.Should().Be(17.25m); // 10.5 + 6.75
		totals?.PlateCountTotal.Should().Be(3);       // 2 + 1
		totals?.PlateAreaTotal.Should().Be(4.75m);  // 3.25 + 1.5
	}

	[Fact]
	public async Task GetOrders_WithFilter_ShouldReturnTotalsOnlyForFilteredData()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Totals_Filtered");
		var client = customFactory.CreateClient();

		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();
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
				o.IsEconom = false;
			})
			.WithProductionOrder(po => { po.OrderId = order1Id; po.CurrentWorkplaceId = workplaceId; })
			.WithOrder(o =>
			{
				o.Id = order2Id;
				o.OrderNumber = "1002";
				o.WindowCount = 3;
				o.WindowArea = 6.75m;
				o.PlateCount = 1;
				o.PlateArea = 1.5m;
				o.IsEconom = true;
			})
			.WithProductionOrder(po => { po.OrderId = order2Id; po.CurrentWorkplaceId = workplaceId; })
			.Build(customFactory.Services);

		// Act — фильтруем только IsEconom = true
		var filters = Uri.EscapeDataString(
			JsonSerializer.Serialize(new[]
			{
				new { field = "IsEconom", value = true, @operator = "eq" }
			}));

		var response = await client.GetAsync($"/api/orders?filters={filters}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();

		var result = JsonSerializer.Deserialize<PaginatedResponse<OrderDto>>(content, new JsonSerializerOptions
		{
			Converters = { new TotalsDtoConverter() },
			PropertyNameCaseInsensitive = true
		});

		// Totals должны учитывать только отфильтрованные записи
		result?.Totals.Should().NotBeNull();
		var totals = (OrderTotalsDto?)result?.Totals;

		totals?.WindowCountTotal.Should().Be(3);  // только 1002
	}

	[Fact]
	public async Task GetOrders_WithNoData_ShouldReturnZeroTotals()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Totals_Empty");
		var client = customFactory.CreateClient();

		// Нет данных в БД

		// Act
		var response = await client.GetAsync("/api/orders");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();

		var result = JsonSerializer.Deserialize<PaginatedResponse<OrderDto>>(content, new JsonSerializerOptions
		{
			Converters = { new TotalsDtoConverter() },
			PropertyNameCaseInsensitive = true
		});

		result?.Totals.Should().BeNull();
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

				services.ConfigureHttpJsonOptions(options =>
				{
					options.SerializerOptions.Converters.Add(new TotalsDtoConverter());
				});

			});
		});
	}
}