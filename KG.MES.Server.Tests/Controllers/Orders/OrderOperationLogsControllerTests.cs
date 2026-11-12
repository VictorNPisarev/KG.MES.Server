using System.Net;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using KG.MES.Server.Tests.Helpers;

namespace KG.MES.Server.Tests.Controllers.Orders;

[Trait("Category", "Orders")]
public class OrderOperationLogsControllerTests : TestBase
{
	public OrderOperationLogsControllerTests(WebApplicationFactory<Program> factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetOrderOperationLogs_ByIdentifier_ShouldReturnLogsSortedDesc()
	{
		// Arrange
		var client = CreateClient();

		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();
		var workplace1Id = Guid.NewGuid();
		var workplace2Id = Guid.NewGuid();
		var userId = Guid.NewGuid();

		var t1 = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var t2 = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);

		BuildTestData(builder => builder
			.WithWorkplace(w => { w.Id = workplace1Id; w.Name = "Торцовка"; w.Code = "CUTTING"; w.IsWorkplace = true; })
			.WithWorkplace(w => { w.Id = workplace2Id; w.Name = "Упаковка"; w.Code = "PACKING"; w.IsWorkplace = true; })
			.WithUser(u => { u.Id = userId; u.Email = "master@test.com"; u.Name = "Мастер"; })
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "OP-001"; })
			.WithProductionOrder(po => { po.Id = productionOrderId; po.OrderId = orderId; })
			.WithOperationLog(ol =>
			{
				ol.ProductionOrderId = productionOrderId;
				ol.WorkplaceId = workplace1Id;
				ol.UserId = userId;
				ol.OperationType = "START";
				ol.OperationTime = t1;
				ol.Notes = "Начало";
				ol.Source = "API";
			})
			.WithOperationLog(ol =>
			{
				ol.ProductionOrderId = productionOrderId;
				ol.WorkplaceId = workplace2Id;
				ol.UserId = null;
				ol.OperationType = "COMPLETE";
				ol.OperationTime = t2;
				ol.Notes = "Авто";
				ol.Source = "auto";
			}));

		// Act — по OrderNumber
		var response = await client.GetAsync("/api/orders/OP-001/operation-logs");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<OrderOperationLogDto>>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Should().HaveCount(2);

		// Свежие сверху
		result[0].OperationType.Should().Be("COMPLETE");
		result[0].WorkplaceName.Should().Be("Упаковка");
		result[0].WorkplaceCode.Should().Be("PACKING");
		result[0].UserName.Should().BeNull();
		result[0].Source.Should().Be("auto");

		result[1].OperationType.Should().Be("START");
		result[1].WorkplaceName.Should().Be("Торцовка");
		result[1].WorkplaceCode.Should().Be("CUTTING");
		result[1].UserName.Should().Be("Мастер");
		result[1].Source.Should().Be("API");
	}

	[Fact]
	public async Task GetOrderOperationLogs_ByGuid_ShouldReturnSameResultAsByNumber()
	{
		// Arrange
		var client = CreateClient();

		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();
		var workplaceId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithWorkplace(w => { w.Id = workplaceId; w.Name = "Сборка"; w.Code = "ASSEMBLY"; w.IsWorkplace = true; })
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "OP-002"; })
			.WithProductionOrder(po => { po.Id = productionOrderId; po.OrderId = orderId; })
			.WithOperationLog(ol =>
			{
				ol.ProductionOrderId = productionOrderId;
				ol.WorkplaceId = workplaceId;
				ol.OperationType = "START";
				ol.OperationTime = DateTime.UtcNow;
				ol.Source = "API";
			}));

		// Act
		var byGuid = await client.GetAsync($"/api/orders/{orderId}/operation-logs");
		var byNumber = await client.GetAsync("/api/orders/OP-002/operation-logs");

		// Assert
		byGuid.StatusCode.Should().Be(HttpStatusCode.OK);
		byNumber.StatusCode.Should().Be(HttpStatusCode.OK);

		var guidContent = await byGuid.Content.ReadAsStringAsync();
		var numberContent = await byNumber.Content.ReadAsStringAsync();

		var guidResult = JsonSerializer.Deserialize<List<OrderOperationLogDto>>(guidContent,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		var numberResult = JsonSerializer.Deserialize<List<OrderOperationLogDto>>(numberContent,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		guidResult.Should().NotBeNull();
		numberResult.Should().NotBeNull();
		guidResult!.Should().HaveCount(1);
		numberResult!.Should().HaveCount(1);
		guidResult[0].Id.Should().Be(numberResult[0].Id);
	}

	[Fact]
	public async Task GetOrderOperationLogs_WithoutProductionOrder_ShouldReturnEmptyList()
	{
		// Arrange
		var client = CreateClient();

		var orderId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "OP-NO-PROD"; }));
		// ProductionOrder НЕ создаём

		// Act
		var response = await client.GetAsync("/api/orders/OP-NO-PROD/operation-logs");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<OrderOperationLogDto>>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Should().BeEmpty();
	}

	[Fact]
	public async Task GetOrderOperationLogs_WhenOrderNotFound_ShouldReturnNotFound()
	{
		// Arrange
		var client = CreateClient();
		var nonExistentOrderId = Guid.NewGuid();

		// Act — по Guid
		var byGuid = await client.GetAsync($"/api/orders/{nonExistentOrderId}/operation-logs");

		// Act — по номеру
		var byNumber = await client.GetAsync("/api/orders/DOES-NOT-EXIST/operation-logs");

		// Assert
		byGuid.StatusCode.Should().Be(HttpStatusCode.NotFound);
		byNumber.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
}