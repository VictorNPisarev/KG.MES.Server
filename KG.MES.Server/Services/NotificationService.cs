using KG.MES.Shared.Data;
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

	/// <summary>
	/// Создать уведомление по заказу для роли
	/// </summary>
	public async Task CreateForRoleAsync(Guid orderId, string eventType, string roleName)
	{
		_context.OrderNotifications.Add(new OrderNotification
		{
			Id = Guid.NewGuid(),
			OrderId = orderId,
			EventType = eventType,
			AudienceType = "ROLE",
			AudienceValue = roleName,
			CreatedAt = DateTime.UtcNow
		});
		await _context.SaveChangesAsync();

		_logger.LogInformation(
			"Order notification: {EventType} for role {RoleName} (OrderId: {OrderId})",
			eventType, roleName, orderId);
	}

	/// <summary>
	/// Создать уведомление по заказу для конкретного пользователя
	/// </summary>
	public async Task CreateForUserAsync(Guid orderId, string eventType, Guid userId)
	{
		_context.OrderNotifications.Add(new OrderNotification
		{
			Id = Guid.NewGuid(),
			OrderId = orderId,
			EventType = eventType,
			AudienceType = "USER",
			AudienceValue = userId.ToString(),
			CreatedAt = DateTime.UtcNow
		});
		await _context.SaveChangesAsync();
	}

	/// <summary>
	/// Создать уведомление для всех
	/// </summary>
	public async Task CreateForAllAsync(Guid orderId, string eventType)
	{
		_context.OrderNotifications.Add(new OrderNotification
		{
			Id = Guid.NewGuid(),
			OrderId = orderId,
			EventType = eventType,
			AudienceType = "ALL",
			AudienceValue = null,
			CreatedAt = DateTime.UtcNow
		});
		await _context.SaveChangesAsync();
	}

	// ============================================================
	// ЧТЕНИЕ И ОТМЕТКА
	// ============================================================

	/// <summary>
	/// Получить непрочитанные уведомления для пользователя
	/// </summary>
	public async Task<List<OrderNotificationDto>> GetUnreadAsync(Guid userId, string? roleName)
	{
		var query = _context.OrderNotifications.AsQueryable();

		// Фильтр по аудитории
		query = query.Where(n =>
			n.AudienceType == "ALL" ||
			(n.AudienceType == "ROLE" && n.AudienceValue == roleName) ||
			(n.AudienceType == "USER" && n.AudienceValue == userId.ToString()));

		// Исключаем прочитанные
		query = query.Where(n => !_context.OrderNotificationReads
			.Any(r => r.NotificationId == n.Id && r.UserId == userId));

		return await query
			.OrderByDescending(n => n.CreatedAt)
			.Select(n => new OrderNotificationDto
			{
				Id = n.Id,
				OrderId = n.OrderId,
				EventType = n.EventType,
				CreatedAt = n.CreatedAt
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
}