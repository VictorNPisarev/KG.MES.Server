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
public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public OrdersControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetOrders_ShouldReturnPaginatedAndSortedData()
	{
		// 1. Arrange (Подготовка)
		var customFactory = SetupTestFactory("TestDb_Orders");
		var client = customFactory.CreateClient();

		var workplaceId = Guid.NewGuid();
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();
		var order3Id = Guid.NewGuid();

		// Создаем тестовые данные через Builder
		new TestDataBuilder()
			.WithWorkplace(w =>
			{
				w.Id = workplaceId;
				w.Name = "Сборка";
				w.IsWorkplace = true;
			})
			.WithOrder(o =>
			{
				o.Id = order1Id;
				o.OrderNumber = "1362";
				o.ReadyDate = DateTime.Parse("2025-06-26T21:00:00.000Z");
				o.WindowCount = 0;
				o.WindowArea = 0;
			})
			.WithProductionOrder(po =>
			{
				po.OrderId = order1Id;
				po.CurrentWorkplaceId = workplaceId;
			})
			.WithOrder(o =>
			{
				o.Id = order2Id;
				o.OrderNumber = "3376";
				o.ReadyDate = DateTime.Parse("2025-07-03T21:00:00.000Z"); // Позже, чем order1
				o.WindowCount = 7;
				o.WindowArea = 13.48m;
			})
			.WithProductionOrder(po =>
			{
				po.OrderId = order2Id;
				po.CurrentWorkplaceId = workplaceId;
			})
			.WithOrder(o =>
			{
				o.Id = order3Id;
				o.OrderNumber = "134";
				o.ReadyDate = DateTime.Parse("2025-07-06T21:00:00.000Z"); // Еще позже
				o.PlateCount = 10;
				o.PlateArea = 0.67m;
			})
			.WithProductionOrder(po =>
			{
				po.OrderId = order3Id;
				po.CurrentWorkplaceId = workplaceId;
			})
			.Build(customFactory.Services);

		// 2. Act (Выполняем запрос с параметрами пагинации и сортировки)
		var url = "/api/orders?page=1&limit=50&sortBy=ready_date&sortOrder=asc";
		var response = await client.GetAsync(url);

		// 3. Assert (Проверки)
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<PaginatedResponse<OrderListItemDto>>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		// Проверяем структуру и данные
		result.Should().NotBeNull();
		result!.Data.Should().HaveCount(3); // Мы создали 3 заказа

		// Проверяем сортировку (по ready_date ASC: order1 → order2 → order3)
		result.Data[0].Id.Should().Be(order1Id);
		result.Data[0].OrderNumber.Should().Be("1362");
		result.Data[0].ReadyDate.Should().Be(DateTime.Parse("2025-06-26T21:00:00.000Z"));

		result.Data[1].Id.Should().Be(order2Id);
		result.Data[1].OrderNumber.Should().Be("3376");
		result.Data[1].WindowCount.Should().Be(7);
		result.Data[1].WindowArea.Should().Be(13.48m);

		result.Data[2].Id.Should().Be(order3Id);
		result.Data[2].OrderNumber.Should().Be("134");
		result.Data[2].PlateCount.Should().Be(10);

		// Проверяем, что CurrentStatus пришел из JOIN с Workplace
		result.Data[0].CurrentWorkplaceId.Should().Be(workplaceId);
		result.Data[0].CurrentStatus.Should().Be("Сборка");

		// Проверяем пагинацию
		result.Pagination.Page.Should().Be(1);
		result.Pagination.Limit.Should().Be(50);
		result.Pagination.Total.Should().Be(3); // Всего в БД 3 заказа
		result.Pagination.Pages.Should().Be(1);

		// Проверяем, что сортировка вернулась в ответе
		result.Sort.By.Should().Be("ready_date");
		result.Sort.Order.Should().Be("asc");
	}

	[Fact]
	public async Task GetOrders_WithWorkplaceFilter_ShouldReturnFilteredData()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Orders_Filter");
		var client = customFactory.CreateClient();

		var workplace1Id = Guid.NewGuid();
		var workplace2Id = Guid.NewGuid();
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();

		new TestDataBuilder()
			.WithWorkplace(w => { w.Id = workplace1Id; w.Name = "Сборка"; })
			.WithWorkplace(w => { w.Id = workplace2Id; w.Name = "Покраска"; })
			.WithOrder(o => { o.Id = order1Id; o.OrderNumber = "100"; })
			.WithProductionOrder(po => { po.OrderId = order1Id; po.CurrentWorkplaceId = workplace1Id; })
			.WithOrder(o => { o.Id = order2Id; o.OrderNumber = "200"; })
			.WithProductionOrder(po => { po.OrderId = order2Id; po.CurrentWorkplaceId = workplace2Id; })
			.Build(customFactory.Services);

		// Act - фильтруем по workplace1
		var url = $"/api/orders?workplaceId={workplace1Id}";
		var response = await client.GetAsync(url);

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<PaginatedResponse<OrderListItemDto>>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Data.Should().HaveCount(1); // Только один заказ на workplace1
		result.Data[0].Id.Should().Be(order1Id);
		result.Data[0].CurrentWorkplaceId.Should().Be(workplace1Id);
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