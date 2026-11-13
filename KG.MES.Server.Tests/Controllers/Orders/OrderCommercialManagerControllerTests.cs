using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using KG.MES.Server.Tests.Helpers;

namespace KG.MES.Server.Tests.Controllers.Orders;

[Trait("Category", "Orders")]
public class OrderCommercialManagerControllerTests : TestBase
{
	public OrderCommercialManagerControllerTests(WebApplicationFactory<Program> factory) : base(factory)
	{
	}

	[Fact]
	public async Task UpdateCommercial_WithManagerId_ShouldSetManager()
	{
		// Arrange
		var client = CreateClient();

		var orderId = Guid.NewGuid();
		var managerId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "MGR-INT-001"; })
			.WithUser(u => { u.Id = managerId; u.Email = "mgr@test.com"; u.Name = "Иван Менеджеров"; u.IsActive = true; }));

		var dto = new OrderCommercialRequestDto
		{
			ManagerId = managerId,
			Amount = 100000m,
			Currency = "RUB"
		};

		// Act
		var response = await client.PutAsJsonAsync($"/api/orders/{orderId}/commercial", dto);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderCommercialDto>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.ManagerId.Should().Be(managerId);
		result.ManagerName.Should().Be("Иван Менеджеров");
		result.Amount.Should().Be(100000m);
	}

	[Fact]
	public async Task UpdateCommercial_WithManagerName_ShouldResolveManagerByName()
	{
		// Arrange
		var client = CreateClient();

		var orderId = Guid.NewGuid();
		var managerId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "MGR-INT-002"; })
			.WithUser(u => { u.Id = managerId; u.Email = "petr@test.com"; u.Name = "Пётр Продавцов"; u.IsActive = true; }));

		var dto = new OrderCommercialRequestDto
		{
			ManagerName = "пётр продавцов",  // lower case — должно найтись
			Amount = 200000m,
			Currency = "RUB"
		};

		// Act
		var response = await client.PutAsJsonAsync($"/api/orders/{orderId}/commercial", dto);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderCommercialDto>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.ManagerId.Should().Be(managerId);
		result.ManagerName.Should().Be("Пётр Продавцов");  // нормализовано
	}

	[Fact]
	public async Task UpdateCommercial_WithNonExistentManagerId_ShouldReturnServerError()
	{
		// Arrange
		var client = CreateClient();

		var orderId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "MGR-INT-003"; }));

		var dto = new OrderCommercialRequestDto
		{
			ManagerId = Guid.NewGuid(),  // не существует
			Amount = 50000m
		};

		// Act
		var response = await client.PutAsJsonAsync($"/api/orders/{orderId}/commercial", dto);

		// Assert
		// Сервис кидает Exception → 500 (обработка ошибок не добавлена, как договорились)
		response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
	}

	[Fact]
	public async Task UpdateCommercial_WithNonExistentManagerName_ShouldReturnServerError()
	{
		// Arrange
		var client = CreateClient();

		var orderId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "MGR-INT-004"; }));

		var dto = new OrderCommercialRequestDto
		{
			ManagerName = "Несуществующий Менеджер",
			Amount = 75000m
		};

		// Act
		var response = await client.PutAsJsonAsync($"/api/orders/{orderId}/commercial", dto);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
	}

	[Fact]
	public async Task UpdateCommercial_WithoutManager_ShouldKeepManagerNull()
	{
		// Arrange
		var client = CreateClient();

		var orderId = Guid.NewGuid();

		BuildTestData(builder => builder
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "MGR-INT-005"; }));

		var dto = new OrderCommercialRequestDto
		{
			ManagerId = null,
			ManagerName = null,
			Amount = 99999m,
			Currency = "RUB"
		};

		// Act
		var response = await client.PutAsJsonAsync($"/api/orders/{orderId}/commercial", dto);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderCommercialDto>(content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.ManagerId.Should().BeNull();
		result.ManagerName.Should().BeNull();
		result.Amount.Should().Be(99999m);
	}
}