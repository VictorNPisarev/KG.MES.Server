
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
	private bool _isLoading;

	private async Task Save()
	{
		_error = "";
		_success = "";

		if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
		{
			_error = "Заполните все поля";
			return;
		}

		if (newPassword != confirmPassword)
		{
			_error = "Пароли не совпадают";
			return;
		}

		if (newPassword.Length < 6)
		{
			_error = "Пароль должен быть не менее 6 символов";
			return;
		}

		_isLoading = true;
		var (success, error) = await AuthService.ChangePasswordAsync(currentPassword, newPassword);
		_isLoading = false;

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
		}
	}

	private async Task Close()
	{
		isOpen = false;
		await OnClose.InvokeAsync();
	}
}