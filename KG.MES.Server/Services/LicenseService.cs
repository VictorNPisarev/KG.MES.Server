// KG.MES.Server/Services/LicenseService.cs
using KG.MES.Server.Data;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public class LicenseService : ILicenseService
{
	private readonly AppDbContext _context;
	private readonly ILogger<LicenseService> _logger;

	public LicenseService(AppDbContext context, ILogger<LicenseService> logger)
	{
		_context = context;
		_logger = logger;
	}

	public async Task<LicenseValidationResult> ValidateAndBindAsync(
		string licenseKey,
		string deviceHardwareId,
		string deviceName,
		string? ipAddress)
	{
		if (string.IsNullOrEmpty(licenseKey))
			return LicenseValidationResult.Fail("License key is required");

		if (string.IsNullOrEmpty(deviceHardwareId))
			return LicenseValidationResult.Fail("Device ID is required");

		var license = await _context.Licenses
			.Include(l => l.Device)
			.FirstOrDefaultAsync(l => l.KeyCode == licenseKey);

		if (license == null)
			return LicenseValidationResult.Fail("Invalid license key");

		if (!license.IsActive)
			return LicenseValidationResult.Fail("License is inactive or revoked");

		if (license.ExpiresAt.HasValue && license.ExpiresAt < DateTime.UtcNow)
			return LicenseValidationResult.Fail("License has expired");

		// Привязка к устройству
		if (license.Device == null)
		{
			var device = new Device
			{
				Id = Guid.NewGuid(),
				DeviceHardwareId = deviceHardwareId,
				DeviceName = deviceName,
				LicenseId = license.Id,
				RegisteredAt = DateTime.UtcNow,
				LastUsedAt = DateTime.UtcNow,
				LastIp = ipAddress
			};

			_context.Devices.Add(device);
			await _context.SaveChangesAsync();

			_logger.LogInformation(
				"🔐 License {LicenseKey} bound to device {DeviceId}",
				licenseKey, deviceHardwareId);

			return LicenseValidationResult.Success(license.Id, device.Id, deviceName);
		}

		// Проверка, что устройство совпадает
		if (license.Device.DeviceHardwareId != deviceHardwareId)
		{
			_logger.LogWarning(
				"⚠️ License {LicenseKey} already used on device '{ExistingDevice}', " +
				"attempt from '{NewDevice}'",
				licenseKey, license.Device.DeviceHardwareId, deviceHardwareId);

			return LicenseValidationResult.Fail(
				$"License already used on device '{license.Device.DeviceName}'");
		}

		// Обновляем время использования
		license.Device.LastUsedAt = DateTime.UtcNow;
		license.Device.LastIp = ipAddress;
		license.Device.DeviceName = deviceName;
		await _context.SaveChangesAsync();

		return LicenseValidationResult.Success(license.Id, license.Device.Id, license.Device.DeviceName);
	}

	public async Task<License?> GetByKeyAsync(string licenseKey)
	{
		return await _context.Licenses
			.Include(l => l.Device)
			.FirstOrDefaultAsync(l => l.KeyCode == licenseKey);
	}

	public async Task<License?> GetByIdAsync(Guid licenseId)
	{
		return await _context.Licenses
			.Include(l => l.Device)
			.FirstOrDefaultAsync(l => l.Id == licenseId);
	}

	public async Task<bool> IsActiveAsync(Guid licenseId)
	{
		var license = await _context.Licenses
			.Where(l => l.Id == licenseId)
			.Select(l => new { l.IsActive, l.ExpiresAt })
			.FirstOrDefaultAsync();

		if (license == null)
			return false;

		if (!license.IsActive)
			return false;

		if (license.ExpiresAt.HasValue && license.ExpiresAt < DateTime.UtcNow)
			return false;

		return true;
	}

	public async Task<bool> RevokeAsync(Guid licenseId, string? reason = null)
	{
		var license = await _context.Licenses.FindAsync(licenseId);
		if (license == null)
			return false;

		license.IsActive = false;
		license.RevokedAt = DateTime.UtcNow;
		license.Notes = reason ?? license.Notes;

		await _context.SaveChangesAsync();

		_logger.LogInformation("🔒 License {LicenseId} revoked. Reason: {Reason}", licenseId, reason ?? "No reason");
		return true;
	}

	public async Task<License> CreateAsync(string? notes = null, int? expiresDays = 30)
	{
		var keyCode = GenerateLicenseKey();

		var license = new License
		{
			Id = Guid.NewGuid(),
			KeyCode = keyCode,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			ExpiresAt = expiresDays.HasValue ? DateTime.UtcNow.AddDays(expiresDays.Value) : null,
			Notes = notes
		};

		_context.Licenses.Add(license);
		await _context.SaveChangesAsync();

		_logger.LogInformation("📦 License {LicenseKey} created", keyCode);
		return license;
	}

	private static string GenerateLicenseKey()
	{
		const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
		var random = Random.Shared;
		var parts = new string[4];
		for (int i = 0; i < 4; i++)
		{
			var part = new char[4];
			for (int j = 0; j < 4; j++)
			{
				part[j] = chars[random.Next(chars.Length)];
			}
			parts[i] = new string(part);
		}
		return string.Join("-", parts);
	}
}