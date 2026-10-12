using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class RegisterRequestDto
{
	[JsonPropertyName("registrationToken")]
	public string RegistrationToken { get; set; } = string.Empty;

	[JsonPropertyName("fullName")]
	public string FullName { get; set; } = string.Empty;

	[JsonPropertyName("password")]
	public string Password { get; set; } = string.Empty;

	[JsonPropertyName("licenseKey")]
	public string LicenseKey { get; set; } = string.Empty;

	[JsonPropertyName("deviceHardwareId")]
	public string DeviceHardwareId { get; set; } = string.Empty;

	[JsonPropertyName("applicationCode")]
	public string? ApplicationCode { get; set; }

}