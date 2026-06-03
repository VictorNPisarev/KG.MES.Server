using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("comments")]
public class Comment
{
	[Column("id")] public Guid Id { get; set; }
	[Column("order_id")] public Guid OrderId { get; set; }
	[Column("user_id")] public Guid? UserId { get; set; }
	[Column("content")] public string Content { get; set; } = string.Empty;
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("updated_at")] public DateTime UpdatedAt { get; set; }

	[ForeignKey("OrderId")]
	public Order? Order { get; set; }

	[ForeignKey("UserId")]
	public User? User { get; set; }
}