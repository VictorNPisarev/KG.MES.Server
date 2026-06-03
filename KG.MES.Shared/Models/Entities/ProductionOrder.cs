using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("production_orders")]
public class ProductionOrder
{
	[Column("id")] public Guid Id { get; set; }
	[Column("legacy_id")] public string? LegacyId { get; set; }
	[Column("order_id")] public Guid OrderId { get; set; }
	[Column("current_workplace_id")] public Guid? CurrentWorkplaceId { get; set; }
	[Column("comment")] public string? Comment { get; set; }
	[Column("lumber")] public string? Lumber { get; set; }
	[Column("glazing_bead")] public string? GlazingBead { get; set; }
	[Column("is_two_side_paint")] public bool IsTwoSidePaint { get; set; }
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("updated_at")] public DateTime UpdatedAt { get; set; }

	[Column("comment_ids", TypeName = "uuid[]")]
	public List<Guid>? CommentIds { get; set; }

	[ForeignKey("OrderId")]
	public Order? Order { get; set; }

	[ForeignKey("CurrentWorkplaceId")]
	public Workplace? CurrentWorkplace { get; set; }

	public ICollection<OrderFootprint>? OrderFootprints { get; set; }
	public ICollection<OperationLog>? OperationLogs { get; set; }
	public ICollection<OrderBlock>? OrderBlocks { get; set; }
}