using KG.MES.Shared.Helpers;
using KG.MES.Shared.Models.Config;
using KG.MES.Shared.Models.ViewModels;
using KG.MES.UI.Shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KG.MES.UI.Shared.Components.Widgets;

public partial class OrderFieldsWidget<TOrder> : ComponentBase, ISavableWidget
{
	[Parameter] public TOrder? Order { get; set; }
	[Parameter] public OrderViewSettings Settings { get; set; } = new();
	[Parameter] public bool ShowSettings { get; set; }

	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;

	private List<ColumnInfo> fieldInfos = [];
	private List<ColumnSetting> fieldSettings = [];
	private bool isSettingsOpen;

	private string cardKey => $"fields_{typeof(TOrder).Name}";

	public bool HasUnsavedChanges() => false;
	public Task SaveAllAsync() => Task.CompletedTask;

	protected override async Task OnInitializedAsync()
	{
		fieldInfos = ColumnHelper.GetColumns<TOrder>();
		await LoadSettings();
		isSettingsOpen = ShowSettings;
	}

	private async Task LoadSettings()
	{
		try
		{
			var json = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"card_settings_{cardKey}");
			fieldSettings = TableSettingsManager.GetSettings<TOrder>(json);
		}
		catch
		{
			fieldSettings = TableSettingsManager.GetDefaultSettings<TOrder>();
		}
	}

	private List<(ColumnInfo Info, ColumnSetting Setting)> BuildVisibleFields()
	{
		return fieldSettings
			.Where(s => s.Visible)
			.Join(fieldInfos, s => s.PropertyName, i => i.PropertyName, (s, i) => (Info: i, Setting: s))
			.OrderBy(x => x.Setting.Order)
			.ToList();
	}

	private string GetFieldClass(int width) => width switch
	{
		4 => "col-md-4 mb-3",
		6 => "col-md-6 mb-3",
		8 => "col-md-8 mb-3",
		12 => "col-12 mb-3",
		_ => "col-md-6 mb-3"
	};

	private async Task ToggleField(string propertyName, bool visible)
	{
		var setting = fieldSettings.FirstOrDefault(s => s.PropertyName == propertyName);
		if (setting != null)
		{
			setting.Visible = visible;
			await SaveSettings();
			StateHasChanged();
		}
	}

	private async Task MoveField(string propertyName, int direction)
	{
		var setting = fieldSettings.FirstOrDefault(s => s.PropertyName == propertyName);
		if (setting == null) return;
		var ordered = fieldSettings.OrderBy(s => s.Order).ToList();
		var idx = ordered.IndexOf(setting);
		var newIdx = idx + direction;
		if (newIdx < 0 || newIdx >= ordered.Count) return;
		(ordered[idx].Order, ordered[newIdx].Order) = (ordered[newIdx].Order, ordered[idx].Order);
		await SaveSettings();
		StateHasChanged();
	}

	private async Task ResetFields()
	{
		fieldSettings = TableSettingsManager.GetDefaultSettings<TOrder>();
		await SaveSettings();
		StateHasChanged();
	}

	private async Task SaveSettings()
	{
		var json = TableSettingsManager.Serialize(fieldSettings);
		await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"card_settings_{cardKey}", json);
	}
}