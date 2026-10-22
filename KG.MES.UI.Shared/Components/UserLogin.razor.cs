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
	
	private string email = "";
	private string password = "";
	private string error = "";
	private string licenseKey = "";
	private string licenseKeyHandle = "";
	private bool isLoading;
	private bool isAuthorized;
	private bool registrationRequired;
	private string? registrationToken;


	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		var license = await LicenseService.LoadLicenseAsync();

		if (license != null)
		{
			licenseKey = license.LicenseKey;
			licenseKeyHandle = "";
		}

		StateHasChanged();
	}

	private async Task Login()
	{
		licenseKey = !string.IsNullOrEmpty(licenseKeyHandle) ? licenseKeyHandle : licenseKey;

		registrationRequired = false;


		if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
		{
			error = "Заполните все поля";
			return;
		}

		// Если ключ не найден — запрашиваем у пользователя
		if (string.IsNullOrEmpty(licenseKey))
		{
			error = "Введите лицензионный ключ";
			return;
		}

		isLoading = true;
		error = "";
		StateHasChanged();

		try
		{
			var request = new LoginRequestDto
			{
				Email = email,
				Password = password,
				LicenseKey = licenseKey,
				DeviceHardwareId = await LicenseService.GetDeviceIdAsync(),
				DeviceName = "Browser"
			};

			var response = await AuthService.LoginAsync(request);

			isLoading = false;

			if (response?.RegistrationRequired == true)
			{
				registrationRequired = true;
				registrationToken = response.RegistrationToken;
				StateHasChanged();
				return;
			}

			if (response != null && !string.IsNullOrEmpty(response.AccessToken))
			{
				var deviceId = await LicenseService.GetDeviceIdAsync();
				Session.SetSession(response, licenseKey, deviceId);

				// Сохраняю в localStorage через сервис
				await Session.PersistAsync();

				// Лицензию тоже сохраняем, если нужно
				if (!string.IsNullOrEmpty(licenseKey))
					await JSRuntime.InvokeVoidAsync("localStorage.setItem", "license_key", licenseKey);

				NavManager.NavigateTo(NavManager.BaseUri);
			}
			else
			{

				error = AuthService.LastError ?? "Authorisation error. Server response = null";

				if (error.ToLower().Contains("license"))
				{
					await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "license_key");
					licenseKey = string.Empty;
					//_licenseKeyHandle = string.Empty;
				}
			}
		}
		catch (Exception ex)
		{
			error = $"Ошибка: {ex.Message}";
		}
		finally
		{
			isLoading = false;
			StateHasChanged();
		}
	}

	private void CancelRegistration()
	{
		registrationRequired = false;
		registrationToken = null;
	}

	private void GoToRegistration()
	{
		NavManager.NavigateTo($"{NavManager.BaseUri}registration?email={email}&token={Uri.EscapeDataString(registrationToken!)}");
	}

}