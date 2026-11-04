using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("notification_events")]
public class NotificationEvent
{
	[Key]
	[Column("id")]
	public Guid Id { get; set; }

	[Column("event_code")]
	[MaxLength(50)]
	public string EventCode { get; set; } = string.Empty;

	[Column("description")]
	[MaxLength(255)]
	public string Description { get; set; } = string.Empty;
}