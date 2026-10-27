using System.Security.Claims;
using KG.MES.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

public partial class NotificationsController
{
	private readonly NotificationService _notificationService;

	public NotificationsController(NotificationService notificationService)
	{
		_notificationService = notificationService;
	}

	public async Task<IActionResult> GetUnreadOrderNotificationsHandler()
	{
		var userId = GetCurrentUserId();
		var roleName = User.FindFirst(ClaimTypes.Role)?.Value;

		var notifications = await _notificationService.GetUnreadAsync(userId, roleName);
		return Ok(notifications);
	}

	public async Task<IActionResult> MarkOrderNotificationReadHandler(Guid notificationId)
	{
		var userId = GetCurrentUserId();
		await _notificationService.MarkReadAsync(notificationId, userId);
		return Ok();
	}

	public async Task<IActionResult> MarkOrderNotificationsReadHandler(Guid orderId)
	{
		if (!CurrentUserId.HasValue || string.IsNullOrEmpty(CurrentRoleName))
			return Unauthorized();

		await _notificationService.MarkAllOrderNotificationsReadAsync(
			orderId, CurrentUserId.Value, CurrentRoleName);

		return Ok();
	}

	public async Task<IActionResult> MarkAllOrderNotificationsReadHandler()
	{
		if (!CurrentUserId.HasValue || string.IsNullOrEmpty(CurrentRoleName))
			return Unauthorized();

		var count = await _notificationService.MarkAllOrderNotificationsReadAsync(
			CurrentUserId.Value, CurrentRoleName);

		return Ok(new { markedCount = count });
	}

	// Вспомогательные методы
	private Guid GetCurrentUserId()
	{
		var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
			throw new UnauthorizedAccessException("User ID not found in token");
		return userId;
	}

}