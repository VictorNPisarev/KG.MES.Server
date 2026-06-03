using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("roles")]
public class Role
{
	[Column("id")] public Guid Id { get; set; }
	[Column("legacy_id")] public string? LegacyId { get; set; }
	[Column("name")] public string Name { get; set; } = string.Empty;
	[Column("description")] public string? Description { get; set; }
	[Column("level")] public int Level { get; set; }
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("updated_at")] public DateTime UpdatedAt { get; set; }

	public ICollection<User>? Users { get; set; }
}