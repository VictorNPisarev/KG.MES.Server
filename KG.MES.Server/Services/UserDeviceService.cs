// KG.MES.Server/Services/UserDeviceService.cs
using KG.MES.Server.Data;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KG.MES.Server.Services;

public class UserDeviceService
{
	private readonly AppDbContext _context;
	private readonly ILogger<UserDeviceService> _logger;

	public UserDeviceService(AppDbContext context, ILogger<UserDeviceService> logger)
	{
		_context = context;
		_logger = logger;
	}

	/// <summary>
	/// Зарегистрировать новое устройство для пользователя
	/// </summary>
	public async Task<UserDevice> RegisterDeviceAsync(Guid userId, string deviceId, string? deviceName = null, string? activationKey = null)
	{
		// Проверяем, существует ли уже устройство с таким deviceId для этого пользователя
		var existing = await _context.UserDevices
			.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId);

		if (existing != null)
		{
			//TODO если устройство не активно - ключ отозван. Условие не актуально - надо убрать этот кусок
			// Если устройство было неактивным — реактивируем
			if (!existing.IsActive)
			{
				existing.IsActive = true;
				existing.RevokedAt = null;
				//existing.UpdatedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
				_logger.LogInformation("🔐 Device {DeviceId} reactivated for user {UserId}", deviceId, userId);
				return existing;
			}

			// Уже активное устройство — просто обновляем время
			existing.LastUsedAt = DateTime.UtcNow;
			//existing.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			_logger.LogInformation("ℹ️ Device {DeviceId} already registered for user {UserId}", deviceId, userId);
			return existing;
		}

		// Создаём новое устройство
		var device = new UserDevice
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			DeviceId = deviceId,
			DeviceName = deviceName,
			ActivationKey = activationKey,
			IsActive = true,
			IsPrimary = false,
			RegisteredAt = DateTime.UtcNow,
			LastUsedAt = DateTime.UtcNow,
		};

		// Если это первое активное устройство — делаем его основным
		var activeCount = await _context.UserDevices
			.CountAsync(d => d.UserId == userId && d.IsActive);

		if (activeCount == 0)
		{
			device.IsPrimary = true;
		}

		_context.UserDevices.Add(device);
		await _context.SaveChangesAsync();

		_logger.LogInformation("🔐 New device {DeviceId} registered for user {UserId}", deviceId, userId);
		return device;
	}

	/// <summary>
	/// Проверить, активно ли устройство для пользователя
	/// </summary>
	public async Task<bool> IsDeviceActiveAsync(Guid userId, string deviceId)
	{
		var device = await _context.UserDevices
			.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId && d.IsActive);

		if (device != null)
		{
			// Обновляем время последнего использования
			device.LastUsedAt = DateTime.UtcNow;
			//device.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return true;
		}

		return false;
	}

	/// <summary>
	/// Получить устройство по ID
	/// </summary>
	public async Task<UserDevice?> GetDeviceAsync(Guid userId, string deviceId)
	{
		return await _context.UserDevices
			.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId);
	}

	/// <summary>
	/// Получить все устройства пользователя
	/// </summary>
	public async Task<List<UserDevice>> GetUserDevicesAsync(Guid userId)
	{
		return await _context.UserDevices
			.Where(d => d.UserId == userId)
			.OrderByDescending(d => d.IsPrimary)
			.ThenByDescending(d => d.LastUsedAt)
			.ToListAsync();
	}

	/// <summary>
	/// Получить все активные устройства пользователя
	/// </summary>
	public async Task<List<UserDevice>> GetActiveUserDevicesAsync(Guid userId)
	{
		return await _context.UserDevices
			.Where(d => d.UserId == userId && d.IsActive)
			.OrderByDescending(d => d.IsPrimary)
			.ThenByDescending(d => d.LastUsedAt)
			.ToListAsync();
	}

	/// <summary>
	/// Отозвать устройство (заблокировать)
	/// </summary>
	public async Task<bool> RevokeDeviceAsync(Guid deviceId)
	{
		var device = await _context.UserDevices.FindAsync(deviceId);
		if (device == null)
			return false;

		device.IsActive = false;
		device.RevokedAt = DateTime.UtcNow;
		//device.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		_logger.LogInformation("🔒 Device {DeviceId} revoked for user {UserId}", device.DeviceId, device.UserId);
		return true;
	}

	/// <summary>
	/// Отозвать все устройства пользователя
	/// </summary>
	public async Task<int> RevokeAllDevicesAsync(Guid userId)
	{
		var devices = await _context.UserDevices
			.Where(d => d.UserId == userId && d.IsActive)
			.ToListAsync();

		foreach (var device in devices)
		{
			device.IsActive = false;
			device.RevokedAt = DateTime.UtcNow;
			//device.UpdatedAt = DateTime.UtcNow;
		}

		await _context.SaveChangesAsync();

		_logger.LogInformation("🔒 All devices revoked for user {UserId} ({Count} devices)", userId, devices.Count);
		return devices.Count;
	}

	/// <summary>
	/// Установить устройство основным
	/// </summary>
	public async Task<bool> SetPrimaryDeviceAsync(Guid userId, string deviceId)
	{
		// Сбрасываем флаг у всех устройств
		await _context.UserDevices
			.Where(d => d.UserId == userId)
			.ExecuteUpdateAsync(set => set.SetProperty(d => d.IsPrimary, false));

		// Устанавливаем флаг у нужного устройства
		var device = await _context.UserDevices
			.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId);

		if (device == null)
			return false;

		device.IsPrimary = true;
		//device.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		_logger.LogInformation("⭐ Device {DeviceId} set as primary for user {UserId}", deviceId, userId);
		return true;
	}

	/// <summary>
	/// Проверить, включена ли проверка устройств для пользователя
	/// </summary>
	public async Task<bool> IsDeviceCheckEnabledAsync(Guid userId)
	{
		var user = await _context.Users
			.Where(u => u.Id == userId)
			.Select(u => u.IsDeviceCheckEnabled)
			.FirstOrDefaultAsync();

		return user;
	}

	/// <summary>
	/// Включить/выключить проверку устройств для пользователя
	/// </summary>
	public async Task<bool> ToggleDeviceCheckAsync(Guid userId)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null)
			return false;

		user.IsDeviceCheckEnabled = !user.IsDeviceCheckEnabled;
		await _context.SaveChangesAsync();

		_logger.LogInformation("🔧 Device check toggled to {Enabled} for user {UserId}",
			user.IsDeviceCheckEnabled, userId);

		return user.IsDeviceCheckEnabled;
	}

	/// <summary>
	/// Получить статистику устройств пользователя
	/// </summary>
	public async Task<DeviceStatsDto> GetDeviceStatsAsync(Guid userId)
	{
		var allDevices = await _context.UserDevices
			.Where(d => d.UserId == userId)
			.ToListAsync();

		var activeDevices = allDevices.Where(d => d.IsActive).ToList();

		return new DeviceStatsDto
		{
			TotalDevices = allDevices.Count,
			ActiveDevices = activeDevices.Count,
			RevokedDevices = allDevices.Count(d => !d.IsActive),
			PrimaryDevice = activeDevices.FirstOrDefault(d => d.IsPrimary),
			LastUsed = activeDevices.OrderByDescending(d => d.LastUsedAt).FirstOrDefault()
		};
	}
}

/// <summary>
/// DTO для статистики устройств
/// </summary>
public class DeviceStatsDto
{
	public int TotalDevices { get; set; }
	public int ActiveDevices { get; set; }
	public int RevokedDevices { get; set; }
	public UserDevice? PrimaryDevice { get; set; }
	public UserDevice? LastUsed { get; set; }
}