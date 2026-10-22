using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KG.MES.Masters.Components.Layout;
public partial class MainLayout
{
	[Inject] private UserSessionService Session { get; set; } = null!;
	[Inject] IJSRuntime JSRuntime { get; set; } = null!;
	[Inject] NavigationManager NavManager { get; set; } = null!;

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