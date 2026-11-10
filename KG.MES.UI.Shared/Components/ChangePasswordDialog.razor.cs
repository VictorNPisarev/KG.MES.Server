
using Microsoft.AspNetCore.Components;
using KG.MES.Shared.Services;


namespace KG.MES.UI.Shared.Components;

public partial class ChangePasswordDialog 
{

	[Parameter] public EventCallback OnClose { get; set; }

	[Inject] AuthService AuthService { get; set; } = null!;

	private bool isOpen = true;
	private string currentPassword = "";
	private string newPassword = "";
	private string confirmPassword = "";
	private string _error = "";
	private string _success = "";
	private bool isLoading;
	//флаги для подсветки полей
	private bool currentPasswordError;
	private bool newPasswordError;
	private bool confirmPasswordError;

	private async Task Save()
	{
		_error = "";
		_success = "";

		currentPasswordError = string.IsNullOrEmpty(currentPassword);
		newPasswordError = string.IsNullOrEmpty(newPassword);
		confirmPasswordError = string.IsNullOrEmpty(confirmPassword);

		if (currentPasswordError || newPasswordError || confirmPasswordError)
		{
			_error = "Заполните все поля";
			return;
		}

		if (newPassword != confirmPassword)
		{
			newPasswordError = confirmPasswordError = true;
			_error = "Пароли не совпадают";
			return;
		}

		if (newPassword.Length < 6)
		{
			newPasswordError = true;
			_error = "Пароль должен быть не менее 6 символов";
			return;
		}

		isLoading = true;
		var (success, error, currentPassError, newPassError) = await AuthService.ChangePasswordAsync(currentPassword, newPassword);
		isLoading = false;

		if (success)
		{
			_success = "Пароль успешно изменён";
			currentPassword = newPassword = confirmPassword = "";
			await Task.Delay(1500);
			await Close();
		}
		else
		{
			_error = error ?? "Ошибка смены пароля";
			currentPasswordError = currentPassError ?? false;
			newPasswordError = newPassError ?? false;
		}
	}

	private async Task Close()
	{
		isOpen = false;
		await OnClose.InvokeAsync();
	}
}