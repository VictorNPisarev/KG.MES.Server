using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("supply_logs")]
public class SupplyLog
{
	[Key]
	[Column("id")]
	public Guid Id { get; set; }

	[Column("supply_item_id")]
	public Guid SupplyItemId { get; set; }

	[Column("user_id")]
	public Guid? UserId { get; set; }

	[Column("condition_id")]
	public Guid? ConditionId { get; set; }

	[Column("comment")]
	public string? Comment { get; set; }

	[Column("operation_time")]
	public DateTime OperationTime { get; set; }

	[Column("source")]
	[MaxLength(100)]
	public string? Source { get; set; }

	[Column("created_at")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Навигация
	[ForeignKey(nameof(SupplyItemId))]
	public SupplyItem? SupplyItem { get; set; }

	[ForeignKey(nameof(ConditionId))]
	public SupplyCondition? Condition { get; set; }

	[ForeignKey(nameof(UserId))]
	public User? User { get; set; }
}