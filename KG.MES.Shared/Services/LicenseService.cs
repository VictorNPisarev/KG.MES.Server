// KG.MES.Shared/Services/LicenseFileService.cs
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KG.MES.Shared.Models.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Management;


namespace KG.MES.Shared.Services;

public class LicenseService
{
	private readonly ILogger<LicenseService> _logger;
	private const string LicenseFileName = "license.key";
	private readonly IJSRuntime jsRuntime;
	private string licenseKey = string.Empty;
	private string? cachedDeviceId;


	public LicenseService(ILogger<LicenseService> logger, IJSRuntime jsRuntime)
	{
		_logger = logger;
		this.jsRuntime = jsRuntime;
	}

	public async Task<LicenseFileDto?> LoadLicenseAsync()
	{
		try
		{
			licenseKey = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "license_key") ?? "";
			if (!string.IsNullOrEmpty(licenseKey))
			{
				return new LicenseFileDto {LicenseKey = licenseKey};
			}

			// Путь к файлу лицензии (в папке с приложением)
			var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseFileName);

			if (!File.Exists(filePath))
			{
				_logger.LogWarning("License file not found: {FilePath}", filePath);
				return null;
			}

			var json = await File.ReadAllTextAsync(filePath);
			var license = JsonSerializer.Deserialize<LicenseFileDto>(json);

			if (license == null)
			{
				_logger.LogWarning("Failed to parse license file");
				return null;
			}

			// Проверяем срок действия
			if (license.ExpiresAt.HasValue && license.ExpiresAt < DateTime.UtcNow)
			{
				_logger.LogWarning("License expired at {ExpiresAt}", license.ExpiresAt);
				return null;
			}

			_logger.LogInformation("License loaded successfully for {IssuedTo}", license.IssuedTo);
			return license;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading license file");
			return null;
		}
	}

	public async Task<bool> SaveLicenseAsync(LicenseFileDto license, string? filePath = null)
	{
		try
		{
			var path = filePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseFileName);
			var json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = true });
			await File.WriteAllTextAsync(path, json);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving license file");
			return false;
		}
	}

	public async Task<bool> GenerateLicenseAsync(
		string licenseKey,
		string? issuedTo = null,
		int validityDays = 30,
		string? notes = null)
	{
		try
		{
			var license = new LicenseFileDto
			{
				LicenseKey = licenseKey,
				DeviceId = "",
				IssuedAt = DateTime.UtcNow,
				ExpiresAt = DateTime.UtcNow.AddDays(validityDays),
				IssuedTo = issuedTo ?? Environment.UserName,
				Notes = notes
			};

			return await SaveLicenseAsync(license);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error generating license");
			return false;
		}
	}

	//public async Task<string> GetDeviceIdAsync()
	//{
	//	var stored = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "device_id");
	//	if (!string.IsNullOrEmpty(stored)) return stored;

	//	var deviceId = Guid.NewGuid().ToString("N");
	//	await jsRuntime.InvokeVoidAsync("localStorage.setItem", "device_id", deviceId);
	//	return deviceId;
	//}

	// --- Получение DeviceId ---
	public async Task<string> GetDeviceIdAsync()
	{
		// 1. Пытаемся получить ID железа (Windows)
		var hardwareId = GenerateHardwareId();
		if (!string.IsNullOrEmpty(hardwareId))
		{
			// Если железо доступно, всегда используем его (и обновляем кэш в localStorage, если надо)
			if (cachedDeviceId != hardwareId)
			{
				cachedDeviceId = hardwareId;
				try
				{
					await jsRuntime.InvokeVoidAsync("localStorage.setItem", "device_id", hardwareId);
				}
				catch { /* игнорируем ошибки JS, если localStorage недоступен */ }
			}
			return hardwareId;
		}

		// 2. Если железо недоступно (не-Windows), берем из localStorage или генерируем
		if (cachedDeviceId != null) return cachedDeviceId;

		try
		{
			var stored = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "device_id");
			if (!string.IsNullOrEmpty(stored))
			{
				cachedDeviceId = stored;
				return stored;
			}
		}
		catch { }

		var fallbackId = Guid.NewGuid().ToString("N");
		cachedDeviceId = fallbackId;
		try
		{
			await jsRuntime.InvokeVoidAsync("localStorage.setItem", "device_id", fallbackId);
		}
		catch { }

		return fallbackId;
	}

	private string? GenerateHardwareId()
	{
		try
		{
			// Получаем CPU ID
			string cpuId = "";
			using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
			{
				var cpu = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
				cpuId = cpu?["ProcessorId"]?.ToString() ?? "";
			}

			// Получаем MAC-адрес первого активного сетевого интерфейса
			string mac = "";
			var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
				.FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up
									 && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
			if (networkInterface != null)
			{
				mac = networkInterface.GetPhysicalAddress().ToString();
			}

			// Если не удалось получить ни CPU, ни MAC — возвращаем null
			if (string.IsNullOrEmpty(cpuId) && string.IsNullOrEmpty(mac))
				return null;

			// Формируем строку и хешируем
			var combined = $"{cpuId}:{mac}";
			using var sha256 = SHA256.Create();
			var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
			return Convert.ToBase64String(bytes);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to generate hardware ID");
			return null;
		}
	}
}