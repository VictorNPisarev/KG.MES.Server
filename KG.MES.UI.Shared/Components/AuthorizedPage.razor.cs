using System.Text.Json;
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

	private bool? _isAuthorized;

	#region отладочные данные
	private DebugInfo? _debugInfo;
	private int _remainingSeconds;
	private Timer? _timer;
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
		if (_debugInfo == null || _debugInfo.ExpiresAt == null) return;
		_remainingSeconds = (int)((TimeSpan)(_debugInfo.ExpiresAt - DateTime.UtcNow)).TotalSeconds;
		if (_remainingSeconds <= 0) _remainingSeconds = 0;
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
		_timer?.Dispose();
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

		// 1. Если сессия уже в памяти (только что залогинились, без F5)
		if (Session.IsAuthenticated)
		{
			_isAuthorized = true;
			StateHasChanged();
			return;
		}

		// 2. Пробуем восстановить из localStorage
		//var sessionJson = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "session_data");

		await Session.RestoreAsync(JSRuntime);

		//if (string.IsNullOrEmpty(sessionJson))
		if (string.IsNullOrEmpty(Session.AccessToken))
		{
			_isAuthorized = false;
			NavManager.NavigateTo($"{NavManager.BaseUri}login");
			return;
		}

		//SessionData? data;
		//try
		//{
		//	data = JsonSerializer.Deserialize<SessionData>(sessionJson);
		//}
		//catch
		//{
		//	data = null;
		//}

		//if (data == null || string.IsNullOrEmpty(data.RefreshToken))
		//{
		//	await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "session_data");
		//	_isAuthorized = false;
		//	NavManager.NavigateTo($"{NavManager.BaseUri}login");
		//	return;
		//}

		//// Debug info
		var licenseKey = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "license_key") ?? "";
		var deviceId = await LicenseService.GetDeviceIdAsync();


		if (Session.ExpiresAt > DateTime.UtcNow)
		{

			_debugInfo = new DebugInfo
			{
				AccessToken = Session.AccessToken,
				RefreshToken = Session.RefreshToken,
				LicenseKey = Session.LicenseKey,
				DeviceId = Session.DeviceId,
				ExpiresAt = Session.ExpiresAt
			};
			_remainingSeconds = (int)((TimeSpan)(Session.ExpiresAt - DateTime.UtcNow)).TotalSeconds;
			_timer = new Timer(async _ => await UpdateTimer(), null, 0, 1000);

		// 3. Access token ещё жив — восстанавливаем сессию в память
		//if (data.ExpiresAt > DateTime.UtcNow)
		//{
			//Session.SetSession(
			//	new LoginResponseDto
			//	{
			//		AccessToken = data.AccessToken,
			//		RefreshToken = data.RefreshToken,
			//		ExpiresIn = (int)(data.ExpiresAt - DateTime.UtcNow).TotalSeconds,
			//		User = data.User
			//	},
			//	licenseKey,
			//	deviceId
			//);

			_isAuthorized = true;
			StateHasChanged();
			return;
		}

		// 4. Токен протух — пробуем refresh

		if (string.IsNullOrEmpty(Session.RefreshToken))
		{
			_isAuthorized = false;
			NavManager.NavigateTo($"{NavManager.BaseUri}login");
			return;
		}

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

			//var newSessionData = JsonSerializer.Serialize(new
			//{
			//	accessToken = response.AccessToken,
			//	refreshToken = response.RefreshToken,
			//	expiresAt = newExpiresAt,
			//	user = response.User
			//});
			//await JSRuntime.InvokeVoidAsync("localStorage.setItem", "session_data", newSessionData);

			// ← ВОТ ЭТОГО НЕ ХВАТАЛО: восстанавливаем сессию в память
			Session.SetSession(response, licenseKey, deviceId);

			_debugInfo = new DebugInfo
			{
				AccessToken = Session.AccessToken,
				RefreshToken = Session.RefreshToken,
				LicenseKey = Session.LicenseKey,
				DeviceId = Session.DeviceId,
				ExpiresAt = Session.ExpiresAt
			};

			if(_debugInfo.ExpiresAt != null)
			{
				_remainingSeconds = (int)((TimeSpan)(_debugInfo.ExpiresAt - DateTime.UtcNow)).TotalSeconds;
				_timer = new Timer(async _ => await UpdateTimer(), null, 0, 1000);
			}
			await Session.PersistAsync(JSRuntime);

			_isAuthorized = true;
			StateHasChanged();
			return;
		}

		// 5. Refresh не сработал — чистим и на логин
		await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "session_data");
		_isAuthorized = false;
		NavManager.NavigateTo($"{NavManager.BaseUri}login");
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