using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("order_notification_reads")]
public class OrderNotificationRead
{
	[Column("notification_id")]
	public Guid NotificationId { get; set; }

	[Column("user_id")]
	public Guid UserId { get; set; }

	[Column("read_at")]
	public DateTime ReadAt { get; set; } = DateTime.UtcNow;

	// Навигация
	[ForeignKey(nameof(NotificationId))]
	public OrderNotification? Notification { get; set; }

	[ForeignKey(nameof(UserId))]
	public User? User { get; set; }
}