// KG.MES.Server/Services/LicenseService.cs
using KG.MES.Server.Data;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using KG.MES.Shared.Models.Enums;
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
			//return LicenseValidationResult.Fail("License key is required");
			return LicenseValidationResult.Fail("Укажите ключ лицензии");


		if (string.IsNullOrEmpty(deviceHardwareId))
			return LicenseValidationResult.Fail("Device hardware ID is required");

		var license = await _context.Licenses
			.Include(l => l.Devices)
			.FirstOrDefaultAsync(l => l.KeyCode == licenseKey);

		if (license == null)
			//return LicenseValidationResult.Fail("Invalid license key");
			return LicenseValidationResult.Fail("Неверный ключ лицензии");

		if (!license.IsActive)
			//return LicenseValidationResult.Fail("License is inactive or revoked");
			return LicenseValidationResult.Fail("Лицензия не активна или отозвана");

		if (license.ExpiresAt.HasValue && license.ExpiresAt < DateTime.UtcNow)
			//return LicenseValidationResult.Fail("License has expired");
			return LicenseValidationResult.Fail("Лицензия истекла");

		var existingDevice = license.Devices?
			.FirstOrDefault(d => d.DeviceHardwareId == deviceHardwareId);

		if (existingDevice != null)
		{
			// Устройство уже привязано — обновляю время
			existingDevice.LastUsedAt = DateTime.UtcNow;
			existingDevice.LastIp = ipAddress;
			existingDevice.DeviceName = deviceName;
			await _context.SaveChangesAsync();

			return LicenseValidationResult.Success(license.Id, existingDevice.Id, existingDevice.DeviceName);
		}

		// Новое устройство — проверяю лимиты
		if (license.LicenseType == LicenseType.SingleDevice)
		{
			// Строгая лицензия: если уже есть устройство — отказ
			if (license.Devices != null && license.Devices.Any())
			{
				var existing = license.Devices.First();

				_logger.LogWarning(
					"License {LicenseKey} already used on device '{existing.DeviceName}', " +
					"attempt from '{NewDevice}'",
					licenseKey, existing.DeviceHardwareId, deviceHardwareId);

				//return LicenseValidationResult.Fail($"License already used on device '{existing.DeviceName}'");
				return LicenseValidationResult.Fail($"Лицензия занята другим устройством");
			}
		}
		else if (license.LicenseType == LicenseType.MultiDevice)
		{
			// Мультиустройственная: проверяем лимит
			var currentCount = license.Devices?.Count ?? 0;

			// Если MaxDevices == null — безлимитная
			if (license.MaxDevices != null && license.MaxDevices > 0 && currentCount >= license.MaxDevices)
			{
				//return LicenseValidationResult.Fail($"Maximum devices ({license.MaxDevices}) reached for this license");
				return LicenseValidationResult.Fail($"Достигнуто максимальное количество устройств для указанной лицензии");
			}
		}

		// 4. Регистрируем новое устройство
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
			"Device {DeviceId} bound to license {LicenseKey} ({Type})",
			deviceHardwareId, licenseKey, license.LicenseType);

		return LicenseValidationResult.Success(license.Id, device.Id, device.DeviceName);
	}

	public async Task<License?> GetByKeyAsync(string licenseKey)
	{
		return await _context.Licenses
			.Include(l => l.Devices)
			.FirstOrDefaultAsync(l => l.KeyCode == licenseKey);
	}

	public async Task<License?> GetByIdAsync(Guid licenseId)
	{
		return await _context.Licenses
			.Include(l => l.Devices)
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

		_logger.LogInformation("License {LicenseId} revoked. Reason: {Reason}", licenseId, reason ?? "No reason");
		return true;
	}

	public async Task<License> CreateAsync(
	string? notes = null,
	int? expiresDays = 30,
	LicenseType type = LicenseType.SingleDevice,
	int? maxDevices = null)
	{
		var keyCode = GenerateLicenseKey();

		var license = new License
		{
			Id = Guid.NewGuid(),
			KeyCode = keyCode,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			ExpiresAt = expiresDays.HasValue ? DateTime.UtcNow.AddDays(expiresDays.Value) : null,
			Notes = notes,
			LicenseType = type
			// maxDevices останется null, MaxDevices сам вернет 1 для SingleDevice
		};

		// Если MultiDevice — устанавливаем _maxDevices
		if (type == LicenseType.MultiDevice && maxDevices.HasValue)
		{
			license.SetMaxDevices(maxDevices); // ← через метод
		}

		_context.Licenses.Add(license);
		await _context.SaveChangesAsync();

		_logger.LogInformation("License {LicenseKey} created", keyCode);
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

	public async Task<PaginatedResponse<LicenseDto>> GetAllLicensesAsync(
		int page, int limit, string? search, LicenseType? type, bool? isActive)
	{
		var query = _context.Licenses
			.Include(l => l.Devices)
			.AsQueryable();

		if (!string.IsNullOrEmpty(search))
			query = query.Where(l => 
				l.KeyCode.Contains(search) || 
				(l.Notes != null && l.Notes.Contains(search)));

		if (type.HasValue)
			query = query.Where(l => l.LicenseType == type.Value);

		if (isActive.HasValue)
			query = query.Where(l => l.IsActive == isActive.Value);

		var total = await query.CountAsync();

		var items = await query
			.OrderByDescending(l => l.CreatedAt)
			.Skip((page - 1) * limit)
			.Take(limit)
			.Select(l => new LicenseDto
			{
				Id = l.Id,
				KeyCode = l.KeyCode,
				IsActive = l.IsActive,
				LicenseType = l.LicenseType,
				MaxDevices = l.MaxDevices,
				UsedDevices = l.Devices != null ? l.Devices.Count : 0,
				ExpiresAt = l.ExpiresAt,
				CreatedAt = l.CreatedAt,
				Notes = l.Notes
			})
			.ToListAsync();

		return new PaginatedResponse<LicenseDto>
		{
			Data = items,
			Pagination = new PaginationInfo
			{
				Page = page,
				Limit = limit,
				Total = total,
				Pages = (int)Math.Ceiling(total / (double)limit)
			}
		};
	}

	public async Task<LicenseDto?> GetLicenseDetailsAsync(Guid licenseId)
	{
		var license = await _context.Licenses
			.Include(l => l.Devices)
			.FirstOrDefaultAsync(l => l.Id == licenseId);

		if (license == null)
			return null;

		return new LicenseDto
		{
			Id = license.Id,
			KeyCode = license.KeyCode,
			IsActive = license.IsActive,
			CreatedAt = license.CreatedAt,
			ExpiresAt = license.ExpiresAt,
			RevokedAt = license.RevokedAt,
			Notes = license.Notes,
			LicenseType = license.LicenseType,
			MaxDevices = license.MaxDevices,
			UsedDevices = license.Devices?.Count ?? 0,
			//Devices = license.Devices?.Select(d => new DeviceInfoDto
			//{
			//	Id = d.Id,
			//	DeviceHardwareId = d.DeviceHardwareId,
			//	DeviceName = d.DeviceName,
			//	RegisteredAt = d.RegisteredAt,
			//	//LastUsedAt = d.LastUsedAt,
			//	LastIp = d.LastIp
			//}).ToList() ?? new()
		};
	}

	public async Task<List<DeviceInfoDto>> GetLicenseDevicesAsync(Guid licenseId)
	{
		return await _context.Devices
			.Where(d => d.LicenseId == licenseId)
			.Select(d => new DeviceInfoDto
			{
				Id = d.Id,
				DeviceHardwareId = d.DeviceHardwareId,
				DeviceName = d.DeviceName,
				RegisteredAt = d.RegisteredAt,
				//LastUsedAt = d.LastUsedAt,
				LastIp = d.LastIp
			})
			.ToListAsync();
	}

	public async Task<bool> RevokeDeviceAsync(Guid licenseId, Guid deviceId)
	{
		var device = await _context.Devices
			.FirstOrDefaultAsync(d => d.Id == deviceId && d.LicenseId == licenseId);

		if (device == null)
			return false;

		_context.Devices.Remove(device);
		await _context.SaveChangesAsync();

		_logger.LogInformation("🔒 Device {DeviceId} revoked from license {LicenseId}", deviceId, licenseId);
		return true;
	}

	public async Task<bool> ActivateAsync(Guid licenseId)
	{
		var license = await _context.Licenses.FindAsync(licenseId);
		if (license == null)
			return false;

		license.IsActive = true;
		license.RevokedAt = null;
		await _context.SaveChangesAsync();

		_logger.LogInformation("✅ License {LicenseId} activated", licenseId);
		return true;
	}

	public async Task<bool> ExtendLicenseAsync(Guid licenseId, int? daysToAdd)
	{
		var license = await _context.Licenses.FindAsync(licenseId);
		if (license == null)
			return false;
		
		if (daysToAdd != null && daysToAdd > 0)
		{
			var newExpiry = license.ExpiresAt.HasValue
				? license.ExpiresAt.Value.AddDays((int)daysToAdd)
				: DateTime.UtcNow.AddDays((int)daysToAdd);

			license.ExpiresAt = newExpiry;
		}
		else
		{
			license.ExpiresAt = null;
		}

		await _context.SaveChangesAsync();

		_logger.LogInformation("📅 License {LicenseId} extended by {Days} days", licenseId, daysToAdd);
		return true;
	}
}