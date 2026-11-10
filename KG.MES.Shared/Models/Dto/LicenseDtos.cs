using System.Text.Json.Serialization;
using KG.MES.Shared.Models.Enums;

namespace KG.MES.Shared.Models.Dto;

/// <summary>
/// DTO для создания лицензии
/// </summary>
public class CreateLicenseRequestDto
{
	[JsonPropertyName("notes")]
	public string? Notes { get; set; }

	[JsonPropertyName("expiresDays")]
	public int? ExpiresDays { get; set; } = 30;

	[JsonPropertyName("licenseType")]
	public LicenseType LicenseType { get; set; } = LicenseType.SingleDevice;

	[JsonPropertyName("maxDevices")]
	public int? MaxDevices { get; set; }
}

/// <summary>
/// DTO для отзыва лицензии
/// </summary>
public class RevokeLicenseRequestDto
{
	[JsonPropertyName("reason")]
	public string? Reason { get; set; }
}

/// <summary>
/// DTO для продления лицензии
/// </summary>
public class ExtendLicenseRequestDto
{
	[JsonPropertyName("daysToAdd")]
	public int? DaysToAdd { get; set; }
}

/// <summary>
/// DTO для списка лицензий (краткая информация)
/// </summary>
public class LicenseDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("keyCode")]
	public string KeyCode { get; set; } = string.Empty;

	[JsonPropertyName("isActive")]
	public bool IsActive { get; set; }

	[JsonPropertyName("licenseType")]
	public LicenseType LicenseType { get; set; }

	[JsonPropertyName("maxDevices")]
	public int? MaxDevices { get; set; }

	[JsonPropertyName("usedDevices")]
	public int UsedDevices { get; set; }

	[JsonPropertyName("createdAt")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("expiresAt")]
	public DateTime? ExpiresAt { get; set; }

	[JsonPropertyName("revokedAt")]
	public DateTime? RevokedAt { get; set; }

	[JsonPropertyName("notes")]
	public string? Notes { get; set; }
}

/// <summary>
/// DTO для детальной информации о лицензии
/// </summary>
public class LicenseDetailsDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("keyCode")]
	public string KeyCode { get; set; } = string.Empty;

	[JsonPropertyName("isActive")]
	public bool IsActive { get; set; }

	[JsonPropertyName("createdAt")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("expiresAt")]
	public DateTime? ExpiresAt { get; set; }

	[JsonPropertyName("revokedAt")]
	public DateTime? RevokedAt { get; set; }

	[JsonPropertyName("notes")]
	public string? Notes { get; set; }

	[JsonPropertyName("licenseType")]
	public LicenseType LicenseType { get; set; }

	[JsonPropertyName("maxDevices")]
	public int? MaxDevices { get; set; }

	[JsonPropertyName("usedDevices")]
	public int UsedDevices { get; set; }

	[JsonPropertyName("devices")]
	public List<DeviceInfoDto> Devices { get; set; } = [];
}

/// <summary>
/// DTO для информации об устройстве
/// </summary>
public class DeviceInfoDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("deviceHardwareId")]
	public string DeviceHardwareId { get; set; } = string.Empty;

	[JsonPropertyName("deviceName")]
	public string? DeviceName { get; set; }

	[JsonPropertyName("registeredAt")]
	public DateTime RegisteredAt { get; set; }

	[JsonPropertyName("lastUsedAt")]
	public DateTime LastUsedAt { get; set; }

	[JsonPropertyName("lastIp")]
	public string? LastIp { get; set; }
}

/// <summary>
/// DTO для результата создания лицензии
/// </summary>
public class CreatedLicenseDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("keyCode")]
	public string KeyCode { get; set; } = string.Empty;

	[JsonPropertyName("isActive")]
	public bool IsActive { get; set; }

	[JsonPropertyName("licenseType")]
	public LicenseType LicenseType { get; set; }

	[JsonPropertyName("maxDevices")]
	public int? MaxDevices { get; set; }

	[JsonPropertyName("expiresAt")]
	public DateTime? ExpiresAt { get; set; }

	[JsonPropertyName("createdAt")]
	public DateTime CreatedAt { get; set; }
}