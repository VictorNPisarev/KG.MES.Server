using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("order_footprints")]
public class OrderFootprint
{
	[Column("id")] public Guid Id { get; set; }
	[Column("legacy_id")] public string? LegacyId { get; set; }
	[Column("production_order_id")] public Guid ProductionOrderId { get; set; }
	[Column("workplace_id")] public Guid WorkplaceId { get; set; }
	[Column("status")] public string Status { get; set; } = "planned";
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("updated_at")] public DateTime UpdatedAt { get; set; }

	[ForeignKey("ProductionOrderId")]
	public ProductionOrder? ProductionOrder { get; set; }

	[ForeignKey("WorkplaceId")]
	public Workplace? Workplace { get; set; }
}