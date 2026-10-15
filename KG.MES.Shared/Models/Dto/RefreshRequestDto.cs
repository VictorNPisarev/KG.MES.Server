using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class RefreshRequestDto
{
	[JsonPropertyName("refresh_token")]
	public string RefreshToken { get; set; } = string.Empty;

	[JsonPropertyName("device_hardware_id")]
	public string DeviceHardwareId { get; set; } = string.Empty;

	[JsonPropertyName("license_key")]
	public string LicenseKey { get; set; } = string.Empty;
}