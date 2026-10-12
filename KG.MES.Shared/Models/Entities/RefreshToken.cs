using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("refresh_tokens")]
public class RefreshToken
{
	[Column("id")] public Guid Id { get; set; }
	[Column("user_id")] public Guid UserId { get; set; }
	[Column("device_id")] public Guid DeviceId { get; set; }
	[Column("license_id")] public Guid LicenseId { get; set; }
	[Column("token")] public string Token { get; set; } = string.Empty;
	[Column("expires_at")] public DateTime ExpiresAt { get; set; }
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("revoked_at")] public DateTime? RevokedAt { get; set; }
	[Column("is_revoked")] public bool IsRevoked { get; set; }

	[ForeignKey("UserId")]
	public User? User { get; set; }

	[ForeignKey("DeviceId")]
	public Device? Device { get; set; }

	[ForeignKey("LicenseId")]
	public License? License { get; set; }
}