using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

/// <summary>
/// Информация об устройстве (для DTO)
/// </summary>
public class UserDeviceDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("device_id")]
	public string DeviceId { get; set; } = string.Empty;

	[JsonPropertyName("device_name")]
	public string? DeviceName { get; set; }

	[JsonPropertyName("is_primary")]
	public bool IsPrimary { get; set; }

	[JsonPropertyName("last_used_at")]
	public DateTime? LastUsedAt { get; set; }

	[JsonPropertyName("registered_at")]
	public DateTime RegisteredAt { get; set; }
}