using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class LicenseFileDto
{
	[JsonPropertyName("licenseKey")]
	public string LicenseKey { get; set; } = string.Empty;

	[JsonPropertyName("deviceId")]
	public string DeviceId { get; set; } = string.Empty;

	[JsonPropertyName("issuedAt")]
	public DateTime IssuedAt { get; set; }

	[JsonPropertyName("expiresAt")]
	public DateTime? ExpiresAt { get; set; }

	[JsonPropertyName("issuedTo")]
	public string? IssuedTo { get; set; }

	[JsonPropertyName("notes")]
	public string? Notes { get; set; }
}