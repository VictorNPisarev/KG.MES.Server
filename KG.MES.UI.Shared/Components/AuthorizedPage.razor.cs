using System.Text.Json.Serialization;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KG.MES.UI.Shared.Components;

public partial class AuthorizedPage
{
	[Parameter] public RenderFragment? ChildContent { get; set; }

	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;
	[Inject] private LicenseService LicenseService { get; set; } = null!;
	[Inject] private UserSessionService Session { get; set; } = null!;
	[Inject] private AuthService AuthService { get; set; } = null!;

	private bool? isAuthorized;

	#region отладочные данные
	private DebugInfo? debugInfo;
	private int remainingSeconds;
	private Timer? timer;

	private class DebugInfo
	{
		public string? AccessToken { get; set; } = "";
		public string? RefreshToken { get; set; } = "";
		public string? LicenseKey { get; set; } = "";
		public string? DeviceId { get; set; } = "";
		public DateTime? ExpiresAt { get; set; }
	}

	private async Task UpdateTimer()
	{
		if (debugInfo == null || debugInfo.ExpiresAt == null) return;
		remainingSeconds = (int)((TimeSpan)(debugInfo.ExpiresAt - DateTime.UtcNow)).TotalSeconds;
		if (remainingSeconds <= 0) remainingSeconds = 0;
		await InvokeAsync(StateHasChanged);
	}

	private async Task ClearSession()
	{
		await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "session_data");
		await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "license_key");
		await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "refresh_token");
		Session.Clear();
		NavManager.NavigateTo(NavManager.BaseUri + "login", true);
	}

	public void Dispose()
	{
		timer?.Dispose();
	}
	#endregion

	//protected override async Task OnInitializedAsync()
	//{
	//	var token = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "access_token");
	//	_isAuthorized = !string.IsNullOrEmpty(token);
	//}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		//if (!firstRender)
		//	return;

		// если AccessToken пуст, восстанавливаю сессию из хранилища
		if (!Session.IsAuthenticated)
		{
			await Session.RestoreAsync();
		}

		// если AccessToken все еще пуст - перевожу на страницу входа
		if (!Session.IsAuthenticated)
		{
			isAuthorized = false;
			NavManager.NavigateTo($"{NavManager.BaseUri}login");
			return;
		}

		// если AccessToken еще активен - все ОК
		if (Session?.ExpiresAt > DateTime.UtcNow)
		{
			if (!await CheckPasswordSetAsync()) return;

			debugInfo = new DebugInfo
			{
				AccessToken = Session.AccessToken,
				RefreshToken = Session.RefreshToken,
				LicenseKey = Session.LicenseKey,
				DeviceId = Session.DeviceId,
				ExpiresAt = Session.ExpiresAt
			};
			remainingSeconds = (int)((TimeSpan)(Session.ExpiresAt - DateTime.UtcNow)).TotalSeconds;
			timer?.Dispose();
			timer = new Timer(async _ => await UpdateTimer(), null, 0, 1000);

			isAuthorized = true;
			StateHasChanged();
			return;
		}

		// 4. Токен протух — пробую refresh
		var licenseKey = Session?.LicenseKey 
							?? await JSRuntime.InvokeAsync<string>("localStorage.getItem", "license_key") 
							?? (await LicenseService.LoadLicenseAsync())?.LicenseKey 
							?? "";
		var deviceId = await LicenseService.GetDeviceIdAsync();

		//Если RefreshToken пуст - перевожу на страницу входа
		if (string.IsNullOrEmpty(Session?.RefreshToken))
		{
			isAuthorized = false;
			NavManager.NavigateTo($"{NavManager.BaseUri}login");
			return;
		}

		//Запрашиваю новый AccessToken на основе RefreshToken, ключа лицензии и deviceId
		var request = new RefreshRequestDto
		{
			RefreshToken = Session.RefreshToken,
			LicenseKey = Session.LicenseKey ?? licenseKey,
			DeviceHardwareId = Session.DeviceId ?? deviceId
		};

		var response = await AuthService.RefreshAsync(request);

		if (response != null)
		{
			var newExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn);

			// Обновляю сессию свежими данными
			Session.SetSession(response, licenseKey, deviceId);

			debugInfo = new DebugInfo
			{
				AccessToken = Session.AccessToken,
				RefreshToken = Session.RefreshToken,
				LicenseKey = Session.LicenseKey,
				DeviceId = Session.DeviceId,
				ExpiresAt = Session.ExpiresAt
			};

			if(debugInfo.ExpiresAt != null)
			{
				remainingSeconds = (int)((TimeSpan)(debugInfo.ExpiresAt - DateTime.UtcNow)).TotalSeconds;
				timer?.Dispose();
				timer = new Timer(async _ => await UpdateTimer(), null, 0, 1000);
			}

			await Session.PersistAsync();

			if (!await CheckPasswordSetAsync()) return;

			isAuthorized = true;
			StateHasChanged();
			return;
		}

		// 5. Refresh не сработал — чищу хранилище и перевожу на страницу входа
		await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "session_data");
		isAuthorized = false;
		NavManager.NavigateTo($"{NavManager.BaseUri}login");
	}

	private async Task<bool> CheckPasswordSetAsync()
	{
		if (Session.User?.IsPasswordSet == false)
		{
			NavManager.NavigateTo($"{NavManager.BaseUri}set-password");
			return false;
		}
		return true;
	}

	private class SessionData
	{
		[JsonPropertyName("accessToken")] 
		public string AccessToken { get; set; } = "";

		[JsonPropertyName("refreshToken")]
		public string RefreshToken { get; set; } = "";

		[JsonPropertyName("expiresAt")]
		public DateTime ExpiresAt { get; set; }

		[JsonPropertyName("user")]
		public UserDto? User { get; set; }
	}
}