using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("order_commercial")]
public class OrderCommercial
{
	[Column("id")]
	public Guid Id { get; set; }

	[Column("order_id")]
	public Guid OrderId { get; set; }

	[Column("manager_id")]
	public Guid? ManagerId { get; set; }

	[Column("customer_id")]
	public Guid? CustomerId { get; set; }

	[Column("customer_name")]
	public string? CustomerName { get; set; }

	[Column("amount")]
	public decimal? Amount { get; set; }

	[Column("currency")]
	public string? Currency { get; set; } = "RUB";

	[Column("created_at")]
	public DateTime CreatedAt { get; set; }

	[Column("updated_at")]
	public DateTime UpdatedAt { get; set; }

	// Навигация
	[ForeignKey("OrderId")]
	public Order? Order { get; set; }

	[ForeignKey("ManagerId")]
	public User? Manager { get; set; }

	[ForeignKey("CustomerId")]
	public Customer? Customer { get; set; }
}