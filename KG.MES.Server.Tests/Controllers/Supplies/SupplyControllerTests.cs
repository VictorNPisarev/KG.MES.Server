using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Tests.Helpers;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace KG.MES.Server.Tests.Controllers;

[Trait("Category", "Supply")]
public class SupplyControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public SupplyControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetOrderSupplyItems_ShouldReturnCorrectFormatWithNulls()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Supply_Items");
		var client = customFactory.CreateClient();

		var orderId = Guid.NewGuid();
		var orderSupplyId = Guid.NewGuid();
		var lumberTypeId = Guid.NewGuid();
		var glassTypeId = Guid.NewGuid();
		var pendingConditionId = Guid.NewGuid();

		new TestDataBuilder()
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "SUPPLY-1"; })
			.WithOrderSupply(os => { os.Id = orderSupplyId; os.OrderId = orderId; })
			.WithSupplyType(st => { st.Id = lumberTypeId; st.Name = "lumber"; st.IsActive = true; })
			.WithSupplyType(st => { st.Id = glassTypeId; st.Name = "glass"; st.IsActive = true; })
			.WithSupplyCondition(sc => { sc.Id = pendingConditionId; sc.ConditionCode = "pending"; sc.SortOrder = 1; })
			.WithSupplyItem(si =>
			{
				si.OrderSupplyId = orderSupplyId;
				si.SupplyTypeId = lumberTypeId;
				si.ConditionId = pendingConditionId;
				si.Quantity = 10.5m;
				si.ExpectedDate = DateTime.Parse("2026-06-04T10:00:00.000Z");
			})
			.WithSupplyItem(si =>
			{
				si.OrderSupplyId = orderSupplyId;
				si.SupplyTypeId = glassTypeId;
				si.ConditionId = null; // Без статуса, как в Node.js
			})
			.Build(customFactory.Services);

		// Act
		var response = await client.GetAsync($"/api/orders/{orderId}/supplies");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<SupplyOrderListItemDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Should().HaveCount(2);

		var lumber = result.First(r => r.SupplyTypeId == lumberTypeId);
		lumber.SupplyConditionId.Should().Be(pendingConditionId);
		lumber.Quantity.Should().Be(10.5m);

		var glass = result.First(r => r.SupplyTypeId == glassTypeId);
		glass.SupplyConditionId.Should().BeNull(); // Проверяем, что null корректно сериализуется
	}

	[Fact]
	public async Task UpdateAllSupplyItems_ShouldAcceptClientFormatAndReturnSuccess()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Supply_UpdateAll_ClientFormat");
		var client = customFactory.CreateClient();

		var orderId = Guid.NewGuid();
		var orderSupplyId = Guid.NewGuid();
		var lumberTypeId = Guid.NewGuid();
		var paintTypeId = Guid.NewGuid();
		var pendingConditionId = Guid.NewGuid();

		new TestDataBuilder()
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "BATCH-1"; })
			.WithOrderSupply(os => { os.Id = orderSupplyId; os.OrderId = orderId; })
			.WithSupplyType(st => { st.Id = lumberTypeId; st.Name = "lumber"; st.IsActive = true; })
			.WithSupplyType(st => { st.Id = paintTypeId; st.Name = "paint"; st.IsActive = true; })
			.WithSupplyCondition(sc => { sc.Id = pendingConditionId; sc.ConditionCode = "pending"; })
			.WithSupplyItem(si => { si.OrderSupplyId = orderSupplyId; si.SupplyTypeId = lumberTypeId; si.ConditionId = null; })
			.WithSupplyItem(si => { si.OrderSupplyId = orderSupplyId; si.SupplyTypeId = paintTypeId; si.ConditionId = null; })
			.Build(customFactory.Services);

		// Формируем запрос ТОЧНО так, как это делает клиент (обёртка с полем "supplies")
		var clientRequest = new
		{
			supplies = new List<object>
		{
			new { supplyTypeId = lumberTypeId, supplyConditionId = pendingConditionId, comment = "Древесина заказана" },
			new { supplyTypeId = paintTypeId, supplyConditionId = pendingConditionId, comment = "Краска заказана" }
		}
		};

		// Act
		var response = await client.PutAsJsonAsync($"/api/orders/{orderId}/supplies", clientRequest);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OperationResultDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Success.Should().BeTrue();
		result.Message.Should().Be("2 supply items updated");

		// Проверяем, что данные действительно обновились в БД
		using var scope = customFactory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		// Проверяем lumber item
		var lumberItem = await db.SupplyItems.FirstOrDefaultAsync(si => si.SupplyTypeId == lumberTypeId);
		lumberItem.Should().NotBeNull();
		lumberItem!.ConditionId.Should().Be(pendingConditionId);

		// Проверяем, что комментарий создался
		lumberItem.CommentId.Should().NotBeNull("Comment should be created for lumber item");
		var lumberComment = await db.Comments.FirstOrDefaultAsync(c => c.Id == lumberItem.CommentId!.Value);
		lumberComment.Should().NotBeNull();
		lumberComment!.Content.Should().Be("Древесина заказана");
		lumberComment.OrderId.Should().Be(orderId);

		// Проверяем paint item
		var paintItem = await db.SupplyItems.FirstOrDefaultAsync(si => si.SupplyTypeId == paintTypeId);
		paintItem.Should().NotBeNull();
		paintItem!.ConditionId.Should().Be(pendingConditionId);

		// Проверяем, что комментарий создался
		paintItem.CommentId.Should().NotBeNull("Comment should be created for paint item");
		var paintComment = await db.Comments.FirstOrDefaultAsync(c => c.Id == paintItem.CommentId!.Value);
		paintComment.Should().NotBeNull();
		paintComment!.Content.Should().Be("Краска заказана");
		paintComment.OrderId.Should().Be(orderId);
	}

	[Fact]
	public async Task GetAllSupplyItems_ShouldReturnAllOrders()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Supply_All");
		var client = customFactory.CreateClient();

		var orderId1 = Guid.NewGuid(); // Заказ с complete статусами
		var orderId2 = Guid.NewGuid(); // Заказ с pending статусами
		var porductionOrderId1 = Guid.NewGuid(); // Заказ с complete статусами
		var porductionOrderId2 = Guid.NewGuid(); // Заказ с pending статусами
		var orderSupplyId1 = Guid.NewGuid();
		var orderSupplyId2 = Guid.NewGuid();
		var lumberTypeId = Guid.NewGuid();
		var paintTypeId = Guid.NewGuid();
		var completeConditionId = Guid.NewGuid();
		var pendingConditionId = Guid.NewGuid();

		new TestDataBuilder()
			.WithOrder(o => { o.Id = orderId1; o.OrderNumber = "1001"; o.ReadyDate = DateTime.Parse("2026-06-01T10:00:00.000Z"); 
																		o.RtmDate = DateTime.Parse("2026-06-01T10:00:00.000Z"); })
			.WithOrder(o => { o.Id = orderId2; o.OrderNumber = "1002"; o.ReadyDate = DateTime.Parse("2026-06-02T10:00:00.000Z");
																		o.RtmDate = DateTime.Parse("2026-06-01T10:00:00.000Z");
			})
			.WithProductionOrder(po => { po.Id = porductionOrderId1; po.OrderId = orderId1; })
			.WithProductionOrder(po => { po.Id = porductionOrderId2; po.OrderId = orderId2; })
			.WithOrderSupply(os => { os.Id = orderSupplyId1; os.OrderId = orderId1; })
			.WithOrderSupply(os => { os.Id = orderSupplyId2; os.OrderId = orderId2; })
			.WithSupplyType(st => { st.Id = lumberTypeId; st.Name = "lumber"; st.IsActive = true; })
			.WithSupplyType(st => { st.Id = paintTypeId; st.Name = "paint"; st.IsActive = true; })
			.WithSupplyCondition(sc => { sc.Id = completeConditionId; sc.ConditionCode = "complete"; })
			.WithSupplyCondition(sc => { sc.Id = pendingConditionId; sc.ConditionCode = "pending"; })
			// Заказ 1: все complete
			.WithSupplyItem(si => { si.OrderSupplyId = orderSupplyId1; si.SupplyTypeId = lumberTypeId; si.ConditionId = completeConditionId; })
			.WithSupplyItem(si => { si.OrderSupplyId = orderSupplyId1; si.SupplyTypeId = paintTypeId; si.ConditionId = completeConditionId; })
			// Заказ 2: есть pending
			.WithSupplyItem(si => { si.OrderSupplyId = orderSupplyId2; si.SupplyTypeId = lumberTypeId; si.ConditionId = pendingConditionId; })
			.WithSupplyItem(si => { si.OrderSupplyId = orderSupplyId2; si.SupplyTypeId = paintTypeId; si.ConditionId = null; })
			.Build(customFactory.Services);

		// Act
		var response = await client.GetAsync("/api/supplies?page=1&limit=50");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var content = await response.Content.ReadAsStringAsync();
		var json = JsonDocument.Parse(content).RootElement;

		// Проверяем, что вернулись ОБА заказа (и 1001, и 1002)
		json.GetProperty("data").GetArrayLength().Should().Be(2);

		// Проверяем структуру пагинации
		json.GetProperty("pagination").GetProperty("total").GetInt32().Should().Be(2);
		json.GetProperty("pagination").GetProperty("page").GetInt32().Should().Be(1);
		json.GetProperty("pagination").GetProperty("limit").GetInt32().Should().Be(50);
		json.GetProperty("pagination").GetProperty("pages").GetInt32().Should().Be(1);

		// Проверяем, что заказы отсортированы по ready_date
		json.GetProperty("data")[0].GetProperty("order_number").GetString().Should().Be("1001");
		json.GetProperty("data")[1].GetProperty("order_number").GetString().Should().Be("1002");
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
				{
					options.UseInMemoryDatabase(dbName);
					options.ConfigureWarnings(warnings =>
						warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
				});
			});
		});
	}
}