using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("orders")]
public class Order
{
	[Column("id")] public Guid Id { get; set; }
	[Column("legacy_id")] public string? LegacyId { get; set; }
	[Column("order_number")] public string OrderNumber { get; set; } = string.Empty;
	[Column("ready_date")] public DateTime? ReadyDate { get; set; }
	[Column("window_count")] public int WindowCount { get; set; }
	[Column("window_area")] public decimal WindowArea { get; set; }
	[Column("plate_count")] public int PlateCount { get; set; }
	[Column("plate_area")] public decimal PlateArea { get; set; }
	[Column("is_econom")] public bool IsEconom { get; set; }
	[Column("is_claim")] public bool IsClaim { get; set; }
	[Column("is_only_paid")] public bool IsOnlyPaid { get; set; }
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("updated_at")] public DateTime UpdatedAt { get; set; }

	[Column("comment_ids", TypeName = "uuid[]")]
	public List<Guid>? CommentIds { get; set; }

	public ProductionOrder? ProductionOrder { get; set; }
	public OrderSupply? OrderSupply { get; set; }
}