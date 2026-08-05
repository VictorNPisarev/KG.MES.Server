using System.Text.Json.Serialization;

namespace KG.MES.Server.Models.Dto;

public class LoginRequestDto
{
	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	[JsonPropertyName("password")]
	public string Password { get; set; } = string.Empty;

	[JsonPropertyName("license_key")]
	public string LicenseKey { get; set; } = string.Empty;

	[JsonPropertyName("device_id")]
	public string DeviceId { get; set; } = string.Empty;

	[JsonPropertyName("device_name")]
	public string? DeviceName { get; set; }
}