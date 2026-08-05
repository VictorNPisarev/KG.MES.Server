// KG.MES.Shared/Models/Dto/LicenseValidationResult.cs
using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class LicenseValidationResult
{
	[JsonPropertyName("is_valid")]
	public bool IsValid { get; set; }

	[JsonPropertyName("reason")]
	public string? Reason { get; set; }

	[JsonPropertyName("license_id")]
	public Guid? LicenseId { get; set; }

	[JsonPropertyName("device_id")]
	public Guid? DeviceId { get; set; }

	[JsonPropertyName("device_name")]
	public string? DeviceName { get; set; }

	public static LicenseValidationResult Success(Guid licenseId, Guid deviceId, string? deviceName = null) =>
		new()
		{
			IsValid = true,
			LicenseId = licenseId,
			DeviceId = deviceId,
			DeviceName = deviceName
		};

	public static LicenseValidationResult Fail(string reason) =>
		new()
		{
			IsValid = false,
			Reason = reason
		};
}