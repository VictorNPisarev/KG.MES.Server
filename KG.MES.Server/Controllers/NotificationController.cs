using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public partial class NotificationsController : ControllerBase
{
	[HttpGet("orders/unread")]
	public Task<IActionResult> GetUnreadOrderNotifications() => GetUnreadOrderNotificationsHandler();

	[HttpPost("orders/{notificationId}/read")]
	public Task<IActionResult> MarkOrderNotificationRead(Guid notificationId) => MarkOrderNotificationReadHandler(notificationId);
}