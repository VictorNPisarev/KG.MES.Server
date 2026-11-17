using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Services;
using KG.MES.Shared.Model.Enums;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace KG.MES.Server.Tests.Services;

[Trait("Category", "Unit")]
public class NotificationServiceTests : IDisposable
{
	private readonly AppDbContext _context;
	private readonly NotificationService _service;

	public NotificationServiceTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}")
			.ConfigureWarnings(warnings =>
				warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		_context = new AppDbContext(options);
		_service = new NotificationService(_context, new Mock<ILogger<NotificationService>>().Object);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task PublishAsync_ForOrderStarted_WithRoleSubscription_ShouldCreateNotificationForRole()
	{
		// Arrange
		var eventId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var roleId = Guid.NewGuid();

		_context.NotificationEvents.Add(new NotificationEvent
		{
			Id = eventId,
			EventCode = "ORDER_STARTED",
			Description = "Заказ взят в работу"
		});
		_context.NotificationSubscriptions.Add(new NotificationSubscription
		{
			Id = Guid.NewGuid(),
			EventId = eventId,
			RoleId = roleId,
			Severity = "Info",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _context.SaveChangesAsync();

		// Act
		await _service.PublishAsync(NotificationEventType.OrderStarted, orderId);

		// Assert
		var notifications = await _context.OrderNotifications.ToListAsync();
		notifications.Should().HaveCount(1);

		var n = notifications[0];
		n.OrderId.Should().Be(orderId);
		n.EventId.Should().Be(eventId);
		n.RoleId.Should().Be(roleId);
		n.UserId.Should().BeNull();
		n.Severity.Should().Be("Info");
	}

	[Fact]
	public async Task PublishAsync_WithMultipleActiveSubscriptions_ShouldCreateNotificationForEach()
	{
		// Arrange
		var eventId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var roleId = Guid.NewGuid();
		var userId = Guid.NewGuid();

		_context.NotificationEvents.Add(new NotificationEvent
		{
			Id = eventId,
			EventCode = "ORDER_STARTED",
			Description = "Заказ взят в работу"
		});
		_context.NotificationSubscriptions.AddRange(
			new NotificationSubscription
			{
				Id = Guid.NewGuid(),
				EventId = eventId,
				RoleId = roleId,
				Severity = "Warning",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			},
			new NotificationSubscription
			{
				Id = Guid.NewGuid(),
				EventId = eventId,
				UserId = userId,
				Severity = "Danger",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			},
			new NotificationSubscription
			{
				Id = Guid.NewGuid(),
				EventId = eventId,
				// Ни RoleId, ни UserId → подписка "для всех"
				Severity = "Success",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			});
		await _context.SaveChangesAsync();

		// Act
		await _service.PublishAsync(NotificationEventType.OrderStarted, orderId);

		// Assert
		var notifications = await _context.OrderNotifications
			.OrderBy(n => n.Severity)
			.ToListAsync();

		notifications.Should().HaveCount(3);
		notifications.Should().Contain(n => n.RoleId == roleId && n.Severity == "Warning");
		notifications.Should().Contain(n => n.UserId == userId && n.Severity == "Danger");
		notifications.Should().Contain(n => n.RoleId == null && n.UserId == null && n.Severity == "Success");
	}

	[Fact]
	public async Task PublishAsync_WithInactiveSubscriptions_ShouldNotCreateNotifications()
	{
		// Arrange
		var eventId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var roleId = Guid.NewGuid();

		_context.NotificationEvents.Add(new NotificationEvent
		{
			Id = eventId,
			EventCode = "ORDER_STARTED",
			Description = "Заказ взят в работу"
		});
		_context.NotificationSubscriptions.Add(new NotificationSubscription
		{
			Id = Guid.NewGuid(),
			EventId = eventId,
			RoleId = roleId,
			Severity = "Info",
			IsActive = false,  // ← отключена
			CreatedAt = DateTime.UtcNow
		});
		await _context.SaveChangesAsync();

		// Act
		await _service.PublishAsync(NotificationEventType.OrderStarted, orderId);

		// Assert
		(await _context.OrderNotifications.CountAsync()).Should().Be(0);
	}

	[Fact]
	public async Task PublishAsync_WhenEventNotFoundInDb_ShouldNotCreateNotifications()
	{
		// Arrange — событие ORDER_STARTED НЕ добавлено в notification_events
		var orderId = Guid.NewGuid();

		// Act — не должно упасть, только warning
		var act = async () => await _service.PublishAsync(NotificationEventType.OrderStarted, orderId);

		// Assert
		await act.Should().NotThrowAsync();
		(await _context.OrderNotifications.CountAsync()).Should().Be(0);
	}

	[Fact]
	public async Task PublishAsync_ForDifferentEventTypes_ShouldCreateSeparateNotifications()
	{
		// Arrange — два разных события
		var startedEventId = Guid.NewGuid();
		var readyEventId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var roleId = Guid.NewGuid();

		_context.NotificationEvents.AddRange(
			new NotificationEvent { Id = startedEventId, EventCode = "ORDER_STARTED", Description = "Заказ взят в работу" },
			new NotificationEvent { Id = readyEventId, EventCode = "ORDER_READY", Description = "Заказ готов" });

		_context.NotificationSubscriptions.AddRange(
			new NotificationSubscription
			{
				Id = Guid.NewGuid(),
				EventId = startedEventId,
				RoleId = roleId,
				Severity = "Info",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			},
			new NotificationSubscription
			{
				Id = Guid.NewGuid(),
				EventId = readyEventId,
				RoleId = roleId,
				Severity = "Success",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			});
		await _context.SaveChangesAsync();

		// Act
		await _service.PublishAsync(NotificationEventType.OrderStarted, orderId);
		await _service.PublishAsync(NotificationEventType.OrderReady, orderId);

		// Assert
		var notifications = await _context.OrderNotifications.ToListAsync();
		notifications.Should().HaveCount(2);
		notifications.Should().Contain(n => n.EventId == startedEventId && n.Severity == "Info");
		notifications.Should().Contain(n => n.EventId == readyEventId && n.Severity == "Success");
	}
}