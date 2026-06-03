using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("order_supply")]
public class OrderSupply
{
	[Column("id")] public Guid Id { get; set; }
	[Column("order_id")] public Guid OrderId { get; set; }
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("updated_at")] public DateTime UpdatedAt { get; set; }

	[Column("comment_ids", TypeName = "uuid[]")]
	public List<Guid>? CommentIds { get; set; }

	[ForeignKey("OrderId")]
	public Order? Order { get; set; }

	public ICollection<SupplyItem>? SupplyItems { get; set; }
}