using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("devices")]
public class Device
{
	[Key]
	[Column("id")]
	public Guid Id { get; set; }

	[Column("device_hardware_id")]
	[MaxLength(255)]
	[Required]
	public string DeviceHardwareId { get; set; } = string.Empty;

	[Column("device_name")]
	[MaxLength(100)]
	public string? DeviceName { get; set; }

	[Column("license_id")]
	[Required]
	public Guid LicenseId { get; set; }

	[Column("registered_at")]
	[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
	public DateTime RegisteredAt { get; set; }

	[Column("last_used_at")]
	public DateTime? LastUsedAt { get; set; }

	[Column("last_ip")]
	[MaxLength(45)]
	public string? LastIp { get; set; }

	[ForeignKey("LicenseId")]
	public License? License { get; set; }

	public ICollection<UserDevice>? UserDevices { get; set; }
}