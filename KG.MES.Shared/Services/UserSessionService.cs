using System.Text.Json;
using System.Text.Json.Serialization;
using KG.MES.Shared.Models.Dto;
using Microsoft.JSInterop;

namespace KG.MES.Shared.Services;

public class UserSessionService
{
	private LoginResponseDto? loginResponse;

	public string? AccessToken => loginResponse?.AccessToken;
	public string? RefreshToken => loginResponse?.RefreshToken;
	public UserDto? User => loginResponse?.User;
	public DateTime? ExpiresAt { get; private set; }
	public string? LicenseKey { get; private set; }
	public string? DeviceId { get; private set; }

	private readonly IJSRuntime jsRuntime;

	public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);// && User != null;

	public UserSessionService(IJSRuntime jsRuntime)
	{
		this.jsRuntime = jsRuntime;
	}

	public void SetSession(LoginResponseDto response, string licenseKey, string deviceId)
	{
		loginResponse = response;
		LicenseKey = licenseKey;
		DeviceId = deviceId;
		ExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn);
	}

	public void Clear()
	{
		loginResponse = null;
		LicenseKey = null;
		DeviceId = null;
		ExpiresAt = null;
	}

	/// <summary>
	/// Восстанавливает сессию из localStorage при старте circuit (после F5).
	/// </summary>
	public async Task RestoreAsync(IJSRuntime jsRuntime)
	{
		await RestoreAsync();
	}
	public async Task RestoreAsync()
	{
		var json = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "session_data");
		if (string.IsNullOrEmpty(json))
			return;

		try
		{
			var data = JsonSerializer.Deserialize<StoredSession>(json);
			if (data == null)
			{
				//await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "session_data");
				return;
			}

			loginResponse = new LoginResponseDto
			{
				AccessToken = data.AccessToken,
				RefreshToken = data.RefreshToken,
				ExpiresIn = (int)(data.ExpiresAt - DateTime.UtcNow).TotalSeconds,
				User = data.User
			};
			LicenseKey = data.LicenseKey;
			DeviceId = data.DeviceId;
			ExpiresAt = data.ExpiresAt;
		}
		catch
		{
			await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "session_data");
		}
	}

	/// <summary>
	/// Сохраняет сессию в localStorage.
	/// </summary>
	public async Task PersistAsync(IJSRuntime jsRuntime)
	{
		if (loginResponse == null)
			return;

		var expiresAt = DateTime.UtcNow.AddSeconds(loginResponse.ExpiresIn);
		ExpiresAt = expiresAt;  // ← обновляем в памяти


		var data = new StoredSession
		{
			AccessToken = loginResponse.AccessToken,
			RefreshToken = loginResponse.RefreshToken,
			ExpiresAt = expiresAt,
			User = loginResponse.User,
			LicenseKey = LicenseKey,
			DeviceId = DeviceId
		};

		await jsRuntime.InvokeVoidAsync("localStorage.setItem", "session_data",
			JsonSerializer.Serialize(data));
	}

	private class StoredSession
	{

		[JsonPropertyName("accessToken")]
		public string AccessToken { get; set; } = string.Empty;

		[JsonPropertyName("refreshToken")]
		public string RefreshToken { get; set; } = string.Empty;

		[JsonPropertyName("expiresAt")]
		public DateTime ExpiresAt { get; set; }

		[JsonPropertyName("user")]
		public UserDto? User { get; set; }

		[JsonPropertyName("licenseKey")]
		public string? LicenseKey { get; set; }

		[JsonPropertyName("deviceId")]
		public string? DeviceId { get; set; }
	}
}