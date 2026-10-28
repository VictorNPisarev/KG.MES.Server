using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("order_notifications")]
public class OrderNotification
{
	[Key]
	[Column("id")]
	public Guid Id { get; set; }

	[Column("order_id")]
	public Guid OrderId { get; set; }

	[Column("event_id")]
	public Guid EventId { get; set; }

	[Column("role_id")]
	public Guid? RoleId { get; set; }

	[Column("user_id")]
	public Guid? UserId { get; set; }

	[Column("severity")]
	[MaxLength(20)]
	public string Severity { get; set; } = "Info";

	[Column("created_at")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Навигация
	[ForeignKey(nameof(OrderId))]
	public Order? Order { get; set; }

	[ForeignKey(nameof(EventId))]
	public NotificationEvent? Event { get; set; }

	[ForeignKey(nameof(RoleId))]
	public Role? Role { get; set; }

	[ForeignKey(nameof(UserId))]
	public User? User { get; set; }
}
