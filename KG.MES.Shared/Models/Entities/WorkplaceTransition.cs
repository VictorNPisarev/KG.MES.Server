using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("workplace_transitions")]
public class WorkplaceTransition
{
	[Column("id")] public Guid Id { get; set; }
	[Column("legacy_id")] public string? LegacyId { get; set; }
	[Column("from_workplace_id")] public Guid FromWorkplaceId { get; set; }
	[Column("to_workplace_id")] public Guid ToWorkplaceId { get; set; }
	[Column("transition_type")] public string TransitionType { get; set; } = "sequential";
	[Column("created_at")] public DateTime CreatedAt { get; set; }

	[ForeignKey("FromWorkplaceId")]
	public Workplace? FromWorkplace { get; set; }

	[ForeignKey("ToWorkplaceId")]
	public Workplace? ToWorkplace { get; set; }
}