using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public partial class NotificationsController : BaseController
{
	[HttpGet("orders/unread")]
	public Task<IActionResult> GetUnreadOrderNotifications() => GetUnreadOrderNotificationsHandler();

	[HttpPost("{notificationId}/read")]
	public Task<IActionResult> MarkOrderNotificationRead(Guid notificationId) => MarkOrderNotificationReadHandler(notificationId);

	[HttpPost("orders/{orderId}/read")]
	public Task<IActionResult> MarkOrderNotificationsRead(Guid orderId) => MarkOrderNotificationsReadHandler(orderId);

	[HttpPost("orders/read-all")]
	public Task<IActionResult> MarkAllOrderNotificationsRead() => MarkAllOrdersNotificationsReadHandler();
}