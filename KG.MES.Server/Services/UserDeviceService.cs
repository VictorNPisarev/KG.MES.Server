// KG.MES.Server/Services/UserDeviceService.cs
using System.Runtime.InteropServices;
using KG.MES.Server.Data;
using KG.MES.Shared.Models.Dto;
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
	public async Task<Device> RegisterDeviceAsync(Guid userId, string deviceId, string? deviceName = null, string? licenseKey = null)
	{
		// Проверяем, существует ли уже устройство с таким deviceId для этого пользователя
		var existingDevice = await _context.Devices.Include(d => d.License)
			.FirstOrDefaultAsync(d => d.DeviceHardwareId == deviceId);

		if (existingDevice != null)
		{
			// Уже активное устройство — просто обновляем время
			existingDevice.LastUsedAt = DateTime.UtcNow;
			//existing.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			_logger.LogInformation("ℹ️ Device {DeviceId} already registered for user {UserId}", deviceId, userId);
			return existingDevice;
		}

		// Создаём новое устройство
		var device = new Device
		{
			Id = Guid.NewGuid(),
			DeviceHardwareId = deviceId,
			DeviceName = deviceName,
			RegisteredAt = DateTime.UtcNow,
			LastUsedAt = DateTime.UtcNow,
		};

		_context.Devices.Add(device);
		await _context.SaveChangesAsync();

		_logger.LogInformation("🔐 New device {DeviceId} registered for user {UserId}", deviceId, userId);
		return device;
	}

	/// <summary>
	/// Проверить, активно ли устройство для пользователя
	/// </summary>
	public async Task<bool> IsDeviceActiveAsync(Guid userId, string deviceId)
	{
		var device = await _context.Devices
			.FirstOrDefaultAsync(d => d.DeviceHardwareId == deviceId);

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
	public async Task<Device?> GetDeviceAsync(Guid userId, string deviceId)
	{
		return await _context.Devices
			.FirstOrDefaultAsync(d => d.DeviceHardwareId == deviceId);
	}

	/// <summary>
	/// Получить все устройства пользователя
	/// </summary>
	public async Task<List<UserDevice>> GetUserDevicesAsync(Guid userId)
	{
		return await _context.UserDevices
			.Where(d => d.UserId == userId)
			.OrderByDescending(d => d.LastUsedAt)
			.ToListAsync();
	}

	/// <summary>
	/// Получить все активные устройства пользователя
	/// </summary>
	public async Task<List<UserDevice>> GetActiveUserDevicesAsync(Guid userId)
	{
		return await _context.UserDevices
			.Where(d => d.UserId == userId)
			.OrderByDescending(d => d.LastUsedAt)
			.ToListAsync();
	}

	/// <summary>
	/// Отозвать устройство (заблокировать)
	/// </summary>
	public async Task<bool> RevokeDeviceAsync(Guid licenseId)
	{
		var license = await _context.Licenses.FindAsync(licenseId);
		if (license == null)
			return false;

		license.IsActive = false;
		license.RevokedAt = DateTime.UtcNow;
		//device.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		_logger.LogInformation("🔒 License {DeviceId} revoked", license.Id);
		return true;
	}
}