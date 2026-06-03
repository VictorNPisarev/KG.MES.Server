using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("supply_conditions")]
public class SupplyCondition
{
	[Column("id")] public Guid Id { get; set; }
	[Column("condition_code")] public string ConditionCode { get; set; } = string.Empty;
	[Column("display_name")] public string DisplayName { get; set; } = string.Empty;
	[Column("sort_order")] public int SortOrder { get; set; }

	public ICollection<SupplyItem>? SupplyItems { get; set; }
}