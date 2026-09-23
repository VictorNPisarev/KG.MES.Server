
using KG.MES.Shared.Services;


namespace KG.MES.UI.Shared.Components;

public partial class ChangePassword
{
	[Inject] private AuthService AuthService { get; set; } = null!;
	[Inject] private NavigationManager NavManager { get; set; } = null!;

	private string _currentPassword = "";
	private string _newPassword = "";
	private string _confirmPassword = "";
	private string _error = "";
	private string _success = "";
	private bool _isLoading;

	private async Task ChangePassword()
	{
		_error = "";
		_success = "";

		if (string.IsNullOrEmpty(_currentPassword) || string.IsNullOrEmpty(_newPassword))
		{
			_error = "Заполните все поля";
			return;
		}

		if (_newPassword != _confirmPassword)
		{
			_error = "Пароли не совпадают";
			return;
		}

		if (_newPassword.Length < 6)
		{
			_error = "Пароль должен быть не менее 6 символов";
			return;
		}

		_isLoading = true;
		var (success, error) = await AuthService.ChangePasswordAsync(_currentPassword, _newPassword);
		_isLoading = false;

		if (success)
		{
			_success = "Пароль успешно изменён";
			_currentPassword = _newPassword = _confirmPassword = "";
		}
		else
		{
			_error = error ?? "Ошибка смены пароля";
		}
	}
}