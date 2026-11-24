using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Services;
using KG.MES.Shared.Models.Entities;
using KG.MES.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace KG.MES.Server.Tests.Services;

[Trait("Category", "Unit")]
public class SupplyServiceTests : IDisposable
{
	private readonly AppDbContext _context;
	private readonly ServerSupplyService _service;
	private readonly NotificationService _notificationService;

	public SupplyServiceTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}")
			.ConfigureWarnings(warnings =>
				warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new AppDbContext(options);
		_notificationService = new NotificationService(_context, new Mock<ILogger<NotificationService>>().Object);
		_service = new ServerSupplyService(_context, new Mock<ILogger<ServerSupplyService>>().Object, _notificationService);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task GetFavoriteSupplyOrdersAsync_ShouldReturnOnlyUserFavoritesOrderedByCreatedAtDesc()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var otherUserId = Guid.NewGuid();
		var workplaceId = Guid.NewGuid();
		var supplyTypeId = Guid.NewGuid();
		var orderSupplyId1 = Guid.NewGuid();
		var orderSupplyId2 = Guid.NewGuid();
		var orderSupplyId3 = Guid.NewGuid();

		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();
		var order3Id = Guid.NewGuid();  // не в избранном

		_context.Workplaces.Add(new Workplace { Id = workplaceId, Name = "Сборка", IsWorkplace = true });
		_context.SupplyTypes.Add(new SupplyType { Id = supplyTypeId, Name = "lumber", IsActive = true });

		_context.Orders.AddRange(
			new Order { Id = order1Id, OrderNumber = "SPFAV-101" },
			new Order { Id = order2Id, OrderNumber = "SPFAV-102" },
			new Order { Id = order3Id, OrderNumber = "SPFAV-103" });

		_context.ProductionOrders.AddRange(
			new ProductionOrder { Id = Guid.NewGuid(), OrderId = order1Id, CurrentWorkplaceId = workplaceId },
			new ProductionOrder { Id = Guid.NewGuid(), OrderId = order2Id, CurrentWorkplaceId = workplaceId },
			new ProductionOrder { Id = Guid.NewGuid(), OrderId = order3Id, CurrentWorkplaceId = workplaceId });

		_context.OrderSupplies.AddRange(
			new OrderSupply { Id = orderSupplyId1, OrderId = order1Id },
			new OrderSupply { Id = orderSupplyId2, OrderId = order2Id },
			new OrderSupply { Id = orderSupplyId3, OrderId = order3Id });

		_context.SupplyItems.AddRange(
			new SupplyItem { Id = Guid.NewGuid(), OrderSupplyId = orderSupplyId1, SupplyTypeId = supplyTypeId },
			new SupplyItem { Id = Guid.NewGuid(), OrderSupplyId = orderSupplyId2, SupplyTypeId = supplyTypeId },
			new SupplyItem { Id = Guid.NewGuid(), OrderSupplyId = orderSupplyId3, SupplyTypeId = supplyTypeId });

		_context.OrderFavorites.AddRange(
			new OrderFavorite
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				OrderId = order1Id,
				CreatedAt = DateTime.UtcNow.AddHours(-2)
			},
			new OrderFavorite
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				OrderId = order2Id,
				CreatedAt = DateTime.UtcNow.AddHours(-1)
			},
			new OrderFavorite
			{
				Id = Guid.NewGuid(),
				UserId = otherUserId,
				OrderId = order3Id,
				CreatedAt = DateTime.UtcNow
			});

		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetFavoriteSupplyOrdersAsync(userId);

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain(s => s.Id == order1Id);
		result.Should().Contain(s => s.Id == order2Id);
		result.Should().NotContain(s => s.Id == order3Id);

		// Свежие закладки сверху
		result[0].Id.Should().Be(order2Id);
		result[1].Id.Should().Be(order1Id);

		// IsFavorite = true
		result.Should().OnlyContain(s => s.IsFavorite);
	}

	[Fact]
	public async Task GetFavoriteSupplyOrdersAsync_WhenNoFavorites_ShouldReturnEmptyList()
	{
		// Act
		var result = await _service.GetFavoriteSupplyOrdersAsync(Guid.NewGuid());

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}
}