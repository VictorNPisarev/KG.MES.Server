using System.Text.Json;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KG.MES.UI.Shared.Components;

public partial class UserLogin : ComponentBase
{
	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;
	[Inject] private NavigationManager NavManager { get; set; } = null!;
	[Inject] private LicenseService LicenseService { get; set; } = null!;
	[Inject] private UserSessionService Session { get; set; } = null!;
	
	private string _email = "";
	private string _password = "";
	private string _error = "";
	private string _licenseKey = "";
	private string _licenseKeyHandle = "";
	private bool _isLoading;
	private bool _isAuthorized;

	//protected override async Task OnInitializedAsync()
	//{
	//	var license = await LicenseService.LoadLicenseAsync();
		
	//	if (license != null)
	//	{
	//		_licenseKey = license.LicenseKey;
	//		_licenseKeyHandle = "";
	//	}
	//	else
	//	{
	//		// 2. Файла нет — пробую из localStorage
	//		_licenseKey = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "license_key") ?? "";
	//		if (!string.IsNullOrEmpty(_licenseKey))
	//		{
	//			_licenseKeyHandle = ""; // ключ уже есть
	//		}
	//		// Если и там пусто — покажем поле ввода
	//	}
	//}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		var license = await LicenseService.LoadLicenseAsync();

		if (license != null)
		{
			_licenseKey = license.LicenseKey;
			_licenseKeyHandle = "";
		}
		else
		{
			_licenseKey = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "license_key") ?? "";
			if (!string.IsNullOrEmpty(_licenseKey))
			{
				_licenseKeyHandle = "";
			}
		}

		StateHasChanged();
	}

	private async Task Login()
	{
		_licenseKey = !string.IsNullOrEmpty(_licenseKeyHandle) ? _licenseKeyHandle : _licenseKey;
		if (string.IsNullOrEmpty(_email) || string.IsNullOrEmpty(_password))
		{
			_error = "Заполните все поля";
			return;
		}

		// Если ключ не найден — запрашиваем у пользователя
		if (string.IsNullOrEmpty(_licenseKey))
		{
			_error = "Введите лицензионный ключ";
			return;
		}

		_isLoading = true;
		_error = "";
		StateHasChanged();

		try
		{
			var request = new LoginRequestDto
			{
				Email = _email,
				Password = _password,
				LicenseKey = _licenseKey,
				DeviceHardwareId = await LicenseService.GetDeviceIdAsync(),
				DeviceName = "Browser"
			};

			var response = await AuthService.LoginAsync(request);

			if (response != null)
			{
				var deviceId = await LicenseService.GetDeviceIdAsync();
				Session.SetSession(response, _licenseKey, deviceId);

				// Сохраняем в localStorage через сервис
				await Session.PersistAsync(JSRuntime);

				// Лицензию тоже сохраняем, если нужно
				if (!string.IsNullOrEmpty(_licenseKeyHandle))
					await JSRuntime.InvokeVoidAsync("localStorage.setItem", "license_key", _licenseKey);

				NavManager.NavigateTo(NavManager.BaseUri);
			}
			else
			{

				_error = AuthService.LastError ?? "Authorisation error. Server response = null";

				if (_error.ToLower().Contains("license"))
				{
					await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "license_key");
					_licenseKey = string.Empty;
					//_licenseKeyHandle = string.Empty;
				}
			}
		}
		catch (Exception ex)
		{
			_error = $"Ошибка: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}
}