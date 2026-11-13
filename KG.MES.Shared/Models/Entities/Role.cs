using System.ComponentModel.DataAnnotations;
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

	[Column("code")]
	[MaxLength(50)]
	[Required]
	public string Code { get; set; } = string.Empty;

	public ICollection<User>? Users { get; set; }
}