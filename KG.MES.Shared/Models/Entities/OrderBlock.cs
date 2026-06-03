using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("order_blocks")]
public class OrderBlock
{
	[Column("id")] public Guid Id { get; set; }
	[Column("legacy_id")] public string? LegacyId { get; set; }
	[Column("production_order_id")] public Guid ProductionOrderId { get; set; }
	[Column("workplace_id")] public Guid WorkplaceId { get; set; }
	[Column("user_id")] public Guid? UserId { get; set; }
	[Column("reason")] public string? Reason { get; set; }
	[Column("blocked_at")] public DateTime BlockedAt { get; set; }
	[Column("resolved_at")] public DateTime? ResolvedAt { get; set; }
	[Column("resolved_by")] public Guid? ResolvedBy { get; set; }
	[Column("source")] public string? Source { get; set; }

	[ForeignKey("ProductionOrderId")]
	public ProductionOrder? ProductionOrder { get; set; }

	[ForeignKey("WorkplaceId")]
	public Workplace? Workplace { get; set; }

	[ForeignKey("UserId")]
	public User? User { get; set; }

	[ForeignKey("ResolvedBy")]
	public User? ResolvedByUser { get; set; }
}