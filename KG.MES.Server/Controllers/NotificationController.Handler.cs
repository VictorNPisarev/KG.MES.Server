using System.Security.Claims;
using KG.MES.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

public partial class NotificationsController : ControllerBase
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

	// Вспомогательные методы
	private Guid GetCurrentUserId()
	{
		var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
			throw new UnauthorizedAccessException("User ID not found in token");
		return userId;
	}
}