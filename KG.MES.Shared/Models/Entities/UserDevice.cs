using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("user_devices")]

public class UserDevice
{
	[Key]
	[Column("id")]
	public Guid Id { get; set; }

	[Column("user_id")]
	[Required]
	public Guid UserId { get; set; }

	[Column("device_id")]
	[Required]
	public Guid DeviceId { get; set; }

	[Column("created_at")]
	[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
	public DateTime CreatedAt { get; set; }

	[Column("last_used_at")]
	public DateTime? LastUsedAt { get; set; }

	[ForeignKey("UserId")]
	public User? User { get; set; }

	[ForeignKey("DeviceId")]
	public Device? Device { get; set; }
}