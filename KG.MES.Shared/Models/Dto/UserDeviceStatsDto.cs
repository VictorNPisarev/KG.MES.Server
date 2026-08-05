using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

/// <summary>
/// DTO для статистики устройств
/// </summary>
public class UserDeviceStatsDto
{
	[JsonPropertyName("total_devices")]
	public int TotalDevices { get; set; }

	[JsonPropertyName("active_devices")]
	public int ActiveDevices { get; set; }

	[JsonPropertyName("revoked_devices")]
	public int RevokedDevices { get; set; }

	[JsonPropertyName("primary_device")]
	public UserDeviceDto? PrimaryDevice { get; set; }

	[JsonPropertyName("last_used_device")]
	public UserDeviceDto? LastUsedDevice { get; set; }
}