// KG.MES.Shared/Services/LicenseFileService.cs
using System.Text.Json;
using KG.MES.Shared.Models.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace KG.MES.Shared.Services;

public class LicenseService
{
	private readonly ILogger<LicenseService> _logger;
	private const string LicenseFileName = "license.key";
	private readonly IJSRuntime jsRuntime;

	public LicenseService(ILogger<LicenseService> logger, IJSRuntime jsRuntime)
	{
		_logger = logger;
		this.jsRuntime = jsRuntime;
	}

	public async Task<LicenseFileDto?> LoadLicenseAsync()
	{
		try
		{
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

	public async Task<string> GetDeviceIdAsync()
	{
		var stored = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "device_id");
		if (!string.IsNullOrEmpty(stored)) return stored;

		var deviceId = Guid.NewGuid().ToString("N");
		await jsRuntime.InvokeVoidAsync("localStorage.setItem", "device_id", deviceId);
		return deviceId;
	}
}