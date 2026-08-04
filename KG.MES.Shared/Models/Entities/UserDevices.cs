using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("user_devices")]
public class UserDevice
{
	[Column("id")] public Guid Id { get; set; }
	[Column("user_id")] public Guid UserId { get; set; }
	[Column("device_id")] public string DeviceId { get; set; } = string.Empty;
	[Column("device_name")] public string? DeviceName { get; set; }
	[Column("activation_key")] public string? ActivationKey { get; set; }
	[Column("is_active")] public bool IsActive { get; set; } = true;
	[Column("is_primary")] public bool IsPrimary { get; set; }
	[Column("registered_at")] public DateTime RegisteredAt { get; set; }
	[Column("last_used_at")] public DateTime? LastUsedAt { get; set; }
	[Column("last_ip")] public string? LastIp { get; set; }
	[Column("revoked_at")] public DateTime? RevokedAt { get; set; }
	[Column("notes")] public string? Notes { get; set; }

	[ForeignKey("UserId")]
	public User? User { get; set; }
}