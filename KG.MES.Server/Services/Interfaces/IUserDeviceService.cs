// KG.MES.Server/Services/Interfaces/IUserDeviceService.cs
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;

public interface IUserDeviceService
{
	Task<Device> RegisterDeviceAsync(Guid userId, string deviceId, string? deviceName = null);
	Task<bool> IsDeviceActiveAsync(Guid userId, string deviceId);
	Task<Device?> GetDeviceAsync(Guid userId, string deviceId);
	Task<List<UserDevice>> GetUserDevicesAsync(Guid userId);
	Task<List<UserDevice>> GetActiveUserDevicesAsync(Guid userId);
	Task<bool> RevokeDeviceAsync(Guid deviceId);
	Task LinkUserDeviceAsync(Guid userId, Guid deviceId);
}