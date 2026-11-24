using KG.MES.UI.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace KG.MES.UI.Shared.Components;

public partial class RowActionButtons : ComponentBase
{
	[Parameter] public List<RowAction> Actions { get; set; } = [];

	private bool isDrawerOpen;

	/// <summary>
	/// Все действия, которые не Never. Включая Always.
	/// </summary>
	private List<RowAction> DrawerActions =>
		Actions.Where(a => a.Visible && a.HidePolicy != HidePolicy.Never).ToList();

	/// <summary>
	/// Нужен ли drawer:
	/// - есть хотя бы одна Always (удаление), или
	/// - больше одной скрываемой кнопки.
	/// </summary>
	private bool HasDrawer =>
		DrawerActions.Any(a => a.HidePolicy == HidePolicy.Always)
		|| DrawerActions.Count > 1;

	/// <summary>
	/// Кнопки, видимые сразу:
	/// - все Never,
	/// - и все скрываемые, ЕСЛИ drawer не нужен.
	/// </summary>
	private List<RowAction> VisibleActions =>
		Actions
			.Where(a => a.Visible && (a.HidePolicy == HidePolicy.Never || !HasDrawer))
			.ToList();

	private void ToggleDrawer() => isDrawerOpen = !isDrawerOpen;

	private void CloseDrawer() => isDrawerOpen = false;

	private async Task OnDrawerActionClick(RowAction action)
	{
		CloseDrawer();
		await action.OnClick();
	}
}