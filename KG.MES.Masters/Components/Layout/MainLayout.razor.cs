using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KG.MES.Masters.Components.Layout;
public partial class MainLayout
{
	[Inject] private UserSessionService Session { get; set; } = null!;
	[Inject] IJSRuntime JSRuntime { get; set; } = null!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await Session.RestoreAsync(JSRuntime);
			StateHasChanged(); // перерисовать UI, если IsAuthenticated изменилось
		}
	}
}