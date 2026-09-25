using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KG.MES.Main.Shared;

public partial class MainLayout
{
	[Inject] NavigationManager NavManager { get; set; } = null!;
	[Inject] private UserSessionService Session { get; set; } = null!;
	[Inject] IJSRuntime JSRuntime { get; set; } = null!;

	private bool userMenuOpen;
	private bool showChangePassword;


	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await Session.RestoreAsync();
			StateHasChanged(); // перерисовать UI, если IsAuthenticated изменилось
		}
	}

	private void OpenChangePassword()
	{
		userMenuOpen = false;
		showChangePassword = true;
	}

	private async Task Logout()
	{
		userMenuOpen = false;
		NavManager.NavigateTo($"{NavManager.BaseUri}login");
	}

}
