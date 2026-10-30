using KG.MES.Server.Data;
using KG.MES.Server.Extensions;
using KG.MES.Shared.Model.Enums;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public class NotificationService
{
	private readonly AppDbContext _context;
	private readonly ILogger<NotificationService> _logger;

	public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
	{
		_context = context;
		_logger = logger;
	}

	// ============================================================
	// СОЗДАНИЕ УВЕДОМЛЕНИЙ
	// ============================================================


	public async Task PublishAsync(NotificationEventType eventType, Guid orderId)
	{
		var eventCode = eventType.ToCode();
		var @event = await _context.NotificationEvents
			.FirstOrDefaultAsync(e => e.EventCode == eventCode);

		if (@event == null)
		{
			_logger.LogWarning("Event {EventCode} not found in database", eventCode);
			return;
		}

		var subscriptions = await _context.NotificationSubscriptions
			.Where(s => s.EventId == @event.Id && s.IsActive)
			.ToListAsync();

		foreach (var sub in subscriptions)
		{
			_context.OrderNotifications.Add(new OrderNotification
			{
				Id = Guid.NewGuid(),
				OrderId = orderId,
				EventId = @event.Id,
				RoleId = sub.RoleId,
				UserId = sub.UserId,
				Severity = sub.Severity,
				CreatedAt = DateTime.UtcNow
			});
		}

		await _context.SaveChangesAsync();
		_logger.LogInformation("Published {EventCode} for order {OrderId} to {Count} subscribers", eventCode, orderId, subscriptions.Count);
	}

	// ============================================================
	// ЧТЕНИЕ И ОТМЕТКА
	// ============================================================

	/// <summary>
	/// Получить непрочитанные уведомления для пользователя
	/// </summary>
	public async Task<List<OrderNotificationDto>> GetUnreadAsync(Guid userId, Guid? roleId)
	{
		return await _context.OrderNotifications
			.Where(n =>
				(n.RoleId == null && n.UserId == null) ||
				(n.RoleId != null && n.RoleId == roleId) ||
				(n.RoleId == null && n.UserId != null && n.UserId == userId))
			.Where(n => !_context.OrderNotificationReads.Any(r => r.NotificationId == n.Id && r.UserId == userId))
			.OrderByDescending(n => n.CreatedAt)
			.Select(n => new OrderNotificationDto
			{
				Id = n.Id,
				OrderId = n.OrderId,
				EventCode = n.Event!.EventCode,
				EventDescription = n.Event!.Description,
				Severity = n.Severity,
				NotificationCreatedAt = n.CreatedAt
			})
			.ToListAsync();
	}

	/// <summary>
	/// Пометить уведомление прочитанным
	/// </summary>
	public async Task MarkReadAsync(Guid notificationId, Guid userId)
	{
		var exists = await _context.OrderNotificationReads
			.AnyAsync(r => r.NotificationId == notificationId && r.UserId == userId);

		if (exists) return;

		_context.OrderNotificationReads.Add(new OrderNotificationRead
		{
			NotificationId = notificationId,
			UserId = userId,
			ReadAt = DateTime.UtcNow
		});

		await _context.SaveChangesAsync();

		_logger.LogInformation(
			"Notification {NotificationId} marked as read by user {UserId}",
			notificationId, userId);
	}

	public async Task MarkOrderNotificationsReadAsync(Guid orderId, Guid userId, Guid? roleId)
	{
		var unreadIds = await _context.OrderNotifications
			.Where(n => n.OrderId == orderId)
			.Where(n =>
				(n.RoleId == null && n.UserId == null) ||
				(n.RoleId != null && n.RoleId == roleId) ||
				(n.RoleId == null && n.UserId != null && n.UserId == userId))
			.Where(n => !_context.OrderNotificationReads.Any(r => r.NotificationId == n.Id && r.UserId == userId))
			.Select(n => n.Id)
			.ToListAsync();

		foreach (var id in unreadIds)
		{
			_context.OrderNotificationReads.Add(new OrderNotificationRead
			{
				NotificationId = id,
				UserId = userId,
				ReadAt = DateTime.UtcNow
			});
		}

		await _context.SaveChangesAsync();
	}

	public async Task<int> MarkAllOrdersNotificationsReadAsync(Guid userId, Guid? roleId)
	{
		var unreadIds = await _context.OrderNotifications
			.Where(n =>
				(n.RoleId == null && n.UserId == null) ||
				(n.RoleId != null && n.RoleId == roleId) ||
				(n.RoleId == null && n.UserId != null && n.UserId == userId))
			.Where(n => !_context.OrderNotificationReads.Any(r => r.NotificationId == n.Id && r.UserId == userId))
			.Select(n => n.Id)
			.ToListAsync();

		foreach (var id in unreadIds)
		{
			_context.OrderNotificationReads.Add(new OrderNotificationRead
			{
				NotificationId = id,
				UserId = userId,
				ReadAt = DateTime.UtcNow
			});
		}

		await _context.SaveChangesAsync();
		return unreadIds.Count;
	}

	public async Task<List<Guid>> GetUnreadOrderIdsAsync(Guid userId, Guid? roleId, NotificationEventType eventCode)
	{
		var newOrderIds = await _context.OrderNotifications
			.Where(n => n.Event!.EventCode == eventCode.ToCode())
			.Where(n =>
				(n.RoleId == null && n.UserId == null) ||
				(n.RoleId != null && n.RoleId == roleId) ||
				(n.RoleId == null && n.UserId != null && n.UserId == userId))
			.Where(n => !_context.OrderNotificationReads
				.Any(r => r.NotificationId == n.Id && r.UserId == userId))
			.Select(n => n.OrderId)
			.Distinct()
			.ToListAsync();

		return newOrderIds;
	}

	public async Task<List<OrderNotificationDto>> GetUnreadOrderNotificationsAsync(
	Guid userId, Guid? roleId, NotificationEventType? eventType = null)
	{
		var eventCode = eventType?.ToCode();

		var query = _context.OrderNotifications
			.Where(n =>
				(n.RoleId == null && n.UserId == null) ||
				(n.RoleId != null && n.RoleId == roleId) ||
				(n.RoleId == null && n.UserId != null && n.UserId == userId))
			.Where(n => !_context.OrderNotificationReads
				.Any(r => r.NotificationId == n.Id && r.UserId == userId));

		if (!string.IsNullOrEmpty(eventCode))
			query = query.Where(n => n.Event!.EventCode == eventCode);

		return await query
			.OrderByDescending(n => n.CreatedAt)
			.Select(n => new OrderNotificationDto
			{
				Id = n.Id,
				OrderId = n.OrderId,
				EventCode = n.Event!.EventCode,
				EventDescription = n.Event.Description,
				Severity = n.Severity,
				NotificationCreatedAt = n.CreatedAt
			})
			.ToListAsync();
	}

	/// <summary>
	/// Пометить все непрочитанные уведомления по событию прочитанными
	/// </summary>
	/// <returns>Количество помеченных уведомлений</returns>
	public async Task<int> MarkAllByEventReadAsync(
		Guid userId, Guid? roleId, string eventCode)
	{
		// 1. Находим все непрочитанные уведомления по событию
		var unreadIds = await _context.OrderNotifications
			.Where(n => n.Event!.EventCode == eventCode)
			.Where(n =>
				(n.RoleId == null && n.UserId == null) ||
				(n.RoleId != null && n.RoleId == roleId) ||
				(n.RoleId == null && n.UserId != null && n.UserId == userId))
			.Where(n => !_context.OrderNotificationReads
				.Any(r => r.NotificationId == n.Id && r.UserId == userId))
			.Select(n => n.Id)
			.ToListAsync();

		if (!unreadIds.Any())
			return 0;

		// 2. Добавляем отметки о прочтении
		var reads = unreadIds.Select(id => new OrderNotificationRead
		{
			NotificationId = id,
			UserId = userId,
			ReadAt = DateTime.UtcNow
		});

		_context.OrderNotificationReads.AddRange(reads);
		await _context.SaveChangesAsync();

		_logger.LogInformation(
			"User {UserId} marked {Count} notifications as read for event {EventCode}",
			userId, unreadIds.Count, eventCode);

		return unreadIds.Count;
	}
}