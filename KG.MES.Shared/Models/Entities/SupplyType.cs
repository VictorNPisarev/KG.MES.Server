using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("supply_types")]
public class SupplyType
{
	[Column("id")] public Guid Id { get; set; }
	[Column("name")] public string Name { get; set; } = string.Empty;
	[Column("display_name")] public string DisplayName { get; set; } = string.Empty;
	[Column("unit")] public string? Unit { get; set; }
	[Column("sort_order")] public int SortOrder { get; set; }
	[Column("is_active")] public bool IsActive { get; set; } = true;
	[Column("created_at")] public DateTime CreatedAt { get; set; }

	public ICollection<SupplyItem>? SupplyItems { get; set; }
}