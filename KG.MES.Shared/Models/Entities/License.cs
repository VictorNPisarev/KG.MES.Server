using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("licenses")]
public class License
{
	[Key]
	[Column("id")]
	public Guid Id { get; set; }

	[Column("key_code")]
	[MaxLength(32)]
	[Required]
	public string KeyCode { get; set; } = string.Empty;

	[Column("is_active")]
	[DefaultValue(true)]
	public bool IsActive { get; set; } = true;

	[Column("created_at")]
	[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
	public DateTime CreatedAt { get; set; }

	[Column("expires_at")]
	public DateTime? ExpiresAt { get; set; }

	[Column("revoked_at")]
	public DateTime? RevokedAt { get; set; }

	[Column("notes")]
	public string? Notes { get; set; }

	// Навигационное свойство
	public Device? Device { get; set; }
}