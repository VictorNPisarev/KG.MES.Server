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
	[Column("password_hash")] public string? PasswordHash { get; set; }
	[Column("is_password_set")] public bool IsPasswordSet { get; set; }
	[Column("is_active")] public bool IsActive { get; set; } = true;
	[Column("is_approved")] public bool IsApproved { get; set; } = false;
	[Column("approval_deadline")] public DateTime? ApprovalDeadline { get; set; }
	[Column("approved_by")] public Guid? ApprovedBy { get; set; }
	[Column("approved_at")] public DateTime? ApprovedAt { get; set; }
	[ForeignKey("RoleId")] public Role? Role { get; set; }

	public ICollection<UserWorkplace>? UserWorkplaces { get; set; }
	public ICollection<OrderBlock>? OrderBlocks { get; set; }
	public ICollection<OperationLog>? OperationLogs { get; set; }
	public ICollection<Comment>? Comments { get; set; }
	public ICollection<UserDevice>? UserDevices { get; set; }

}