using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("customers")]
public class Customer
{
	[Column("id")]
	public Guid Id { get; set; }

	[Column("name")]
	public string Name { get; set; } = string.Empty;

	[Column("inn")]
	public string? Inn { get; set; }

	[Column("phone")]
	public string? Phone { get; set; }

	[Column("email")]
	public string? Email { get; set; }

	[Column("address")]
	public string? Address { get; set; }

	[Column("created_at")]
	public DateTime CreatedAt { get; set; }

	[Column("updated_at")]
	public DateTime UpdatedAt { get; set; }

	public ICollection<OrderCommercial>? OrderCommercials { get; set; }
}