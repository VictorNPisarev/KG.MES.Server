using KG.MES.Shared.Services;

namespace KG.MES.UI.Shared.Components;

public partial class SetPassword
{
	[Inject] private AuthService AuthService { get; set; } = null!;
	[Inject] private UserSessionService Session { get; set; } = null!;
	[Inject] private NavigationManager NavManager { get; set; } = null;
	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;


	private string _newPassword = "";
	private string _confirmPassword = "";
	private string _error = "";
	private bool _isLoading;

	private async Task SetPassword()
	{
		_error = "";

		if (string.IsNullOrEmpty(_newPassword) || string.IsNullOrEmpty(_confirmPassword))
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

		if (Session.User == null)
		{
			_error = "Пользователь не найден";
			return;
		}

		_isLoading = true;

		var (success, error) = await AuthService.SetPasswordAsync(Session.User.Email, _newPassword);

		_isLoading = false;

		if (success)
		{
			// Обновляем состояние сессии
			Session.User.IsPasswordSet = true;
			await Session.PersistAsync(JSRuntime);

			NavManager.NavigateTo(NavManager.BaseUri, true);
		}
		else
		{
			_error = error ?? "Ошибка установки пароля";
		}
	}
}