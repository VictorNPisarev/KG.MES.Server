using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("supply_items")]
public class SupplyItem
{
	[Column("id")] public Guid Id { get; set; }
	[Column("order_supply_id")] public Guid OrderSupplyId { get; set; }
	[Column("supply_type_id")] public Guid SupplyTypeId { get; set; }
	[Column("condition_id")] public Guid? ConditionId { get; set; }
	[Column("quantity")] public decimal? Quantity { get; set; }
	[Column("expected_date")] public DateTime? ExpectedDate { get; set; }
	[Column("comment")] public string? Comment { get; set; }
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("updated_at")] public DateTime UpdatedAt { get; set; }
	[Column("comment_id")] public Guid? CommentId { get; set; }

	[ForeignKey("OrderSupplyId")]
	public OrderSupply? OrderSupply { get; set; }

	[ForeignKey("SupplyTypeId")]
	public SupplyType? SupplyType { get; set; }

	[ForeignKey("ConditionId")]
	public SupplyCondition? Condition { get; set; }

	[ForeignKey("CommentId")]
	public Comment? CommentEntity { get; set; }
}