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

	[Column("event_type")]
	[MaxLength(50)]
	public string EventType { get; set; } = string.Empty;

	[Column("audience_type")]
	[MaxLength(50)]
	public string AudienceType { get; set; } = string.Empty;  // "ROLE", "USER", "ALL"

	[Column("audience_value")]
	[MaxLength(100)]
	public string? AudienceValue { get; set; }  // "Supply", "Masters", userId, null для ALL

	[Column("created_at")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Навигация
	[ForeignKey(nameof(OrderId))]
	public Order? Order { get; set; }
}