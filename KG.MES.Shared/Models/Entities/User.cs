using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("users")]
public class User
{
	[Column("id")] public Guid Id { get; set; }
	[Column("legacy_id")] public string? LegacyId { get; set; }
	[Column("email")] public string Email { get; set; } = string.Empty;
	[Column("name")] public string Name { get; set; } = string.Empty;
	[Column("role_id")] public Guid? RoleId { get; set; }
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("updated_at")] public DateTime UpdatedAt { get; set; }

	[ForeignKey("RoleId")]
	public Role? Role { get; set; }

	public ICollection<UserWorkplace>? UserWorkplaces { get; set; }
	public ICollection<OrderBlock>? OrderBlocks { get; set; }
	public ICollection<OperationLog>? OperationLogs { get; set; }
	public ICollection<Comment>? Comments { get; set; }
}