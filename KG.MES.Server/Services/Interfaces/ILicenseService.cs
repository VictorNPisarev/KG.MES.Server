// KG.MES.Server/Services/Interfaces/ILicenseService.cs
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using KG.MES.Shared.Models.Enums;

namespace KG.MES.Server.Services.Interfaces;

public interface ILicenseService
{
	/// <summary>
	/// Проверить лицензию и привязать к устройству
	/// </summary>
	Task<LicenseValidationResult> ValidateAndBindAsync(
		string licenseKey,
		string deviceId,
		string deviceName,
		string? ipAddress);

	/// <summary>
	/// Получить лицензию по ключу
	/// </summary>
	Task<License?> GetByKeyAsync(string licenseKey);

	/// <summary>
	/// Получить лицензию по ID
	/// </summary>
	Task<License?> GetByIdAsync(Guid licenseId);

	/// <summary>
	/// Проверить, активна ли лицензия
	/// </summary>
	Task<bool> IsActiveAsync(Guid licenseId);

	/// <summary>
	/// Отозвать лицензию
	/// </summary>
	Task<bool> RevokeAsync(Guid licenseId, string? reason = null);

	/// <summary>
	/// Создать новую лицензию
	/// </summary>
	Task<License> CreateAsync(string? notes = null, int? expiresDays = 30, LicenseType type = LicenseType.SingleDevice, int? maxDevices = null);

	/// <summary>
	/// Список всех лицензий
	/// </summary>
	/// <param name="page"></param>
	/// <param name="limit"></param>
	/// <param name="search"></param>
	/// <param name="type"></param>
	/// <param name="isActive"></param>
	/// <returns></returns>
	Task<PaginatedResponse<LicenseDto>> GetAllLicensesAsync(
	int page, int limit, string? search, LicenseType? type, bool? isActive);

	/// <summary>
	/// Детали лицензии по ID
	/// </summary>
	/// <param name="licenseId"></param>
	/// <returns></returns>
	Task<LicenseDto?> GetLicenseDetailsAsync(Guid licenseId);

	/// <summary>
	/// Активировать лицензию
	/// </summary>
	/// <param name="licenseId"></param>
	/// <returns></returns>
	Task<bool> ActivateAsync(Guid licenseId);

	/// <summary>
	/// Все устройства, привязанные к лицензии
	/// </summary>
	/// <param name="licenseId"></param>
	/// <returns></returns>
	Task<List<DeviceInfoDto>> GetLicenseDevicesAsync(Guid licenseId);


	//Task<bool> RevokeDeviceFromLicenseAsync(Guid licenseId, Guid deviceId);

	/// <summary>
	/// Продлить лицензию на указанное количество дней
	/// </summary>
	/// <param name="licenseId"></param>
	/// <param name="daysToAdd"></param>
	/// <returns></returns>
	Task<bool> ExtendLicenseAsync(Guid licenseId, int? daysToAdd);

}