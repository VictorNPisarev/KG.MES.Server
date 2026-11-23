using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("order_favorites")]
public class OrderFavorite
{
	[Key]
	[Column("id")]
	public Guid Id { get; set; }

	[Column("user_id")]
	[Required]
	public Guid UserId { get; set; }

	[Column("order_id")]
	[Required]
	public Guid OrderId { get; set; }

	[Column("created_at")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Навигация
	[ForeignKey(nameof(UserId))]
	public User? User { get; set; }

	[ForeignKey(nameof(OrderId))]
	public Order? Order { get; set; }
}