using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("operation_logs")]
public class OperationLog
{
	[Column("id")] public Guid Id { get; set; }
	[Column("legacy_id")] public string? LegacyId { get; set; }
	[Column("production_order_id")] public Guid ProductionOrderId { get; set; }
	[Column("workplace_id")] public Guid WorkplaceId { get; set; }
	[Column("user_id")] public Guid? UserId { get; set; }
	[Column("operation_type")] public string OperationType { get; set; } = string.Empty;
	[Column("operation_time")] public DateTime OperationTime { get; set; }
	[Column("notes")] public string? Notes { get; set; }
	[Column("source")] public string? Source { get; set; }
	[Column("created_at")] public DateTime CreatedAt { get; set; }

	[ForeignKey("ProductionOrderId")]
	public ProductionOrder? ProductionOrder { get; set; }

	[ForeignKey("WorkplaceId")]
	public Workplace? Workplace { get; set; }

	[ForeignKey("UserId")]
	public User? User { get; set; }
}