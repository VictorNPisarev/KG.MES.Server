using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("workplaces")]
public class Workplace
{
	[Column("id")] public Guid Id { get; set; }
	[Column("legacy_id")] public string? LegacyId { get; set; }
	[Column("name")] public string Name { get; set; } = string.Empty;
	[Column("previous_workplace_id")] public Guid? PreviousWorkplaceId { get; set; }
	[Column("is_workplace")] public bool IsWorkplace { get; set; }
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("updated_at")] public DateTime UpdatedAt { get; set; }
	[Column("level")] public int Level { get; set; }


	// Навигационные свойства
	[ForeignKey("PreviousWorkplaceId")]
	public Workplace? PreviousWorkplace { get; set; }

	[InverseProperty("FromWorkplace")]
	public ICollection<WorkplaceTransition>? FromTransitions { get; set; }

	[InverseProperty("ToWorkplace")]
	public ICollection<WorkplaceTransition>? ToTransitions { get; set; }

	public ICollection<OrderFootprint>? OrderFootprints { get; set; }
	public ICollection<OrderBlock>? OrderBlocks { get; set; }
	public ICollection<OperationLog>? OperationLogs { get; set; }
	public ICollection<ProductionOrder>? ProductionOrders { get; set; }
	public ICollection<UserWorkplace>? UserWorkplaces { get; set; }
}