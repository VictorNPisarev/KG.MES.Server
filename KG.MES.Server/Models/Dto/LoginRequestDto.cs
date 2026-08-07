using System.Text.Json.Serialization;

namespace KG.MES.Server.Models.Dto;

public class LoginRequestDto
{
	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	[JsonPropertyName("password")]
	public string Password { get; set; } = string.Empty;

	[JsonPropertyName("licenseKey")]
	public string LicenseKey { get; set; } = string.Empty;

	[JsonPropertyName("deviceHardwareId")]
	public string DeviceHardwareId { get; set; } = string.Empty;

	[JsonPropertyName("deviceName")]
	public string? DeviceName { get; set; }
}