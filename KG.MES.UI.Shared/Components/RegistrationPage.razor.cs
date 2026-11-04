using System.Runtime.CompilerServices;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KG.MES.UI.Shared.Components;

public partial class RegistrationPage
{
	[Parameter] public string? Email { get; set; }
	[Parameter] public string? RegistrationToken { get; set; }

	[Inject] private AuthService AuthService { get; set; } = null!;
	[Inject] private UserSessionService Session { get; set; } = null!;
	[Inject] private NavigationManager NavManager { get; set; } = null!;
	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;
	[Inject] private LicenseService LicenseService { get; set; } = null!;


	private string email = "";
	private string firstName = "";
	private string lastName = "";
	private string error = "";
	private bool isLoading;
	private bool firstNameError;
	private bool lastNameError;
	private bool passUnknown = false;

	protected override void OnInitialized()
	{
		email = Email ?? string.Empty;
		// Декодируем email из токена или запрашиваем у сервера
		// Пока — пустая строка, сервер вернёт email в токене
	}

	private async Task Register()
	{
		error = "";
		firstNameError = string.IsNullOrWhiteSpace(firstName);
		lastNameError = string.IsNullOrWhiteSpace(lastName);
		var deviceHardwareId = await LicenseService.GetDeviceIdAsync();
		var license = await LicenseService.LoadLicenseAsync();

		if (firstNameError || lastNameError)
		{
			error = "Заполните все поля";
			return;
		}

		if (string.IsNullOrEmpty(RegistrationToken))
		{
			error = "Неверный токен регистрации";
			return;
		}

		if (license == null || license.LicenseKey == string.Empty)
		{
			error = "Отсутствует лицензия. Свяжитесь с руководителем.";
			return;

		}

		isLoading = true;

		var request = new RegisterRequestDto
		{
			RegistrationToken = RegistrationToken,
			FullName = $"{firstName} {lastName}",
			DeviceHardwareId = deviceHardwareId,
			LicenseKey = license.LicenseKey
		};

		var result = await AuthService.RegisterAsync(request);
		isLoading = false;

		if (result != null && !string.IsNullOrEmpty(result.AccessToken))
		{
			// Успех — сразу авторизуем
			Session.SetSession(new LoginResponseDto
			{
				AccessToken = result.AccessToken,
				RefreshToken = result.RefreshToken ?? "",
				ExpiresIn = result.ExpiresIn,
				User = result.User
			}, license.LicenseKey, deviceHardwareId);

			await Session.PersistAsync();
			NavManager.NavigateTo(NavManager.BaseUri, true);
		}
		else
		{
			error = AuthService.LastError ?? "Ошибка регистрации";
		}
	}
}