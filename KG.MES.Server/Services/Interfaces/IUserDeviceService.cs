// KG.MES.Server/Services/Interfaces/IUserDeviceService.cs
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;

public interface IUserDeviceService
{
	Task<UserDevice> RegisterDeviceAsync(Guid userId, string deviceId, string? deviceName = null, string? activationKey = null);
	Task<bool> IsDeviceActiveAsync(Guid userId, string deviceId);
	Task<UserDevice?> GetDeviceAsync(Guid userId, string deviceId);
	Task<List<UserDevice>> GetUserDevicesAsync(Guid userId);
	Task<List<UserDevice>> GetActiveUserDevicesAsync(Guid userId);
	Task<bool> RevokeDeviceAsync(Guid deviceId);
	Task<int> RevokeAllDevicesAsync(Guid userId);
	Task<bool> SetPrimaryDeviceAsync(Guid userId, string deviceId);
	Task<bool> IsDeviceCheckEnabledAsync(Guid userId);
	Task<bool> ToggleDeviceCheckAsync(Guid userId);
	Task<UserDeviceStatsDto> GetDeviceStatsAsync(Guid userId);
}