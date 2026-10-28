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
		if (!CurrentUserId.HasValue || !CurrentRoleId.HasValue)
			return Unauthorized();

		var notifications = await _notificationService.GetUnreadAsync(CurrentUserId.Value, CurrentRoleId.Value);
		return Ok(notifications);
	}

	public async Task<IActionResult> MarkOrderNotificationReadHandler(Guid notificationId)
	{
		if (!CurrentUserId.HasValue)
			return Unauthorized(); 
		
		await _notificationService.MarkReadAsync(notificationId, CurrentUserId.Value);
		return Ok();
	}

	public async Task<IActionResult> MarkOrderNotificationsReadHandler(Guid orderId)
	{
		if (!CurrentUserId.HasValue || !CurrentRoleId.HasValue)
			return Unauthorized();

		await _notificationService.MarkOrderNotificationsReadAsync(
			orderId, CurrentUserId.Value, CurrentRoleId.Value);

		return Ok();
	}

	public async Task<IActionResult> MarkAllOrdersNotificationsReadHandler()
	{
		if (!CurrentUserId.HasValue || !CurrentRoleId.HasValue)
			return Unauthorized();

		var count = await _notificationService.MarkAllOrdersNotificationsReadAsync(
			CurrentUserId.Value, CurrentRoleId.Value);

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