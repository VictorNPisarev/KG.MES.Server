using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("user_workplaces")]
public class UserWorkplace
{
	[Column("id")] public Guid Id { get; set; }
	[Column("legacy_id")] public string? LegacyId { get; set; }
	[Column("user_id")] public Guid UserId { get; set; }
	[Column("workplace_id")] public Guid WorkplaceId { get; set; }
	[Column("created_at")] public DateTime CreatedAt { get; set; }

	[ForeignKey("UserId")]
	public User? User { get; set; }

	[ForeignKey("WorkplaceId")]
	public Workplace? Workplace { get; set; }
}