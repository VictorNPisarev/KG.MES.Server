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

namespace KG.MES.Server.Tests.Controllers.Orders;

[Trait("Category", "Orders")]
public class OrderCommentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public OrderCommentsControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetOrderComments_WithMixedUserIds_ShouldReturnAllWithCorrectUserNames()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Comments_Mixed");
		var client = customFactory.CreateClient();

		var orderId = Guid.NewGuid();
		var userId = Guid.NewGuid();
		var commentWithUserId = Guid.NewGuid();
		var commentWithoutUserId = Guid.NewGuid();

		new TestDataBuilder()
			.WithRole(r => { r.Name = "User"; r.Level = 1; })
			.WithUser(u => { u.Id = userId; u.Email = "author@test.com"; u.Name = "Иван Иванов"; })
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "4100"; })
			.WithComment(c =>
			{
				c.Id = commentWithUserId;
				c.OrderId = orderId;
				c.UserId = userId;
				c.Content = "коммент с автором";
				c.CreatedAt = DateTime.UtcNow;
				c.UpdatedAt = DateTime.UtcNow;
			})
			.WithComment(c =>
			{
				c.Id = commentWithoutUserId;
				c.OrderId = orderId;
				c.UserId = null; // <-- Ключевой момент: нет пользователя
				c.Content = "коммент без автора";
				c.CreatedAt = DateTime.UtcNow;
				c.UpdatedAt = DateTime.UtcNow;
			})
			.Build(customFactory.Services);

		// Act
		var response = await client.GetAsync($"/api/orders/{orderId}/comments");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<OrderCommentDto>>(content, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		result.Should().NotBeNull();
		result!.Should().HaveCount(2);

		// Проверяем, что LEFT JOIN сработал и user_name = null для второго коммента
		var commentNoUser = result.First(c => c.Id == commentWithoutUserId);
		commentNoUser.UserName.Should().BeNull();
		commentNoUser.Content.Should().Be("коммент без автора");

		var commentWithUser = result.First(c => c.Id == commentWithUserId);
		commentWithUser.UserName.Should().Be("Иван Иванов");
	}

	[Fact]
	public async Task AddOrderComment_ShouldCreateAndReturnComment()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Comments_Add");
		var client = customFactory.CreateClient();
		var orderId = Guid.NewGuid();

		new TestDataBuilder()
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "5000"; })
			.Build(customFactory.Services);

		// Act
		var requestBody = new AddCommentRequestDto { UserId = null, Content = "тестовый коммент" };
		var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/comments", requestBody);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderCommentDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Content.Should().Be("тестовый коммент");
		result.UserName.Should().BeNull(); // Так как UserId был null
		result.Id.Should().NotBeEmpty();
	}

	[Fact]
	public async Task AddProductionOrderComment_ShouldCreateComment()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Comments_Prod");
		var client = customFactory.CreateClient();

		var orderId = Guid.NewGuid();
		var productionOrderId = Guid.NewGuid();

		new TestDataBuilder()
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "6000"; })
			.WithProductionOrder(po => { po.Id = productionOrderId; po.OrderId = orderId; })
			.Build(customFactory.Services);

		// Act
		var requestBody = new AddProductionOrderCommentRequestDto
		{
			ProductionOrderId = productionOrderId,
			UserId = null,
			Content = "коммент к производству"
		};
		var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/productionOrderComments", requestBody);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderCommentDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Content.Should().Be("коммент к производству");
	}

	[Fact]
	public async Task AddSupplyComment_ShouldCreateCommentForSupplyItem()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Comments_Supply");
		var client = customFactory.CreateClient();

		var orderId = Guid.NewGuid();
		var supplyTypeId = Guid.NewGuid();
		var orderSupplyId = Guid.NewGuid();

		new TestDataBuilder()
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "7000"; })
			.WithOrderSupply(os => { os.Id = orderSupplyId; os.OrderId = orderId; })
			.WithSupplyItem(si => { si.OrderSupplyId = orderSupplyId; si.SupplyTypeId = supplyTypeId; })
			.Build(customFactory.Services);

		// Act
		var requestBody = new AddSupplyCommentRequestDto
		{
			SupplyTypeId = supplyTypeId,
			UserId = null,
			Content = "коммент к снабжению"
		};
		var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/OrderSupplyComments", requestBody);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderCommentDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Content.Should().Be("коммент к снабжению");
	}

	[Fact]
	public async Task UpdateOrderComment_ShouldUpdateContent()
	{
		// Arrange
		var customFactory = SetupTestFactory("TestDb_Comments_Update");
		var client = customFactory.CreateClient();

		var orderId = Guid.NewGuid();
		var commentId = Guid.NewGuid();

		new TestDataBuilder()
			.WithOrder(o => { o.Id = orderId; o.OrderNumber = "8000"; })
			.WithComment(c => { c.Id = commentId; c.OrderId = orderId; c.Content = "старый текст"; })
			.Build(customFactory.Services);

		// Act
		var requestBody = new UpdateCommentRequestDto { Content = "новый текст" };
		var response = await client.PutAsJsonAsync($"/api/orders/{orderId}/comments/{commentId}", requestBody);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var content = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OrderCommentDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		result.Should().NotBeNull();
		result!.Content.Should().Be("новый текст");
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
					// Игнорируем предупреждение о транзакциях в InMemory
					options.ConfigureWarnings(warnings =>
						warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
				});
			});
		});
	}
}