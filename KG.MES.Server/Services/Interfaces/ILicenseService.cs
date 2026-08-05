// KG.MES.Server/Services/Interfaces/ILicenseService.cs
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;

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
	Task<License> CreateAsync(string? notes = null, int? expiresDays = 30);
}