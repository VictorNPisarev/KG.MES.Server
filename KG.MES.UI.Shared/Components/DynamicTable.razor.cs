using System.Reflection;
using System.Text.Json;
using KG.MES.Shared.Helpers;
using KG.MES.Shared.Models;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KG.MES.UI.Shared.Components;
public partial class DynamicTable<TListItem> : ComponentBase
{
	[Parameter] public IEnumerable<TListItem> Items { get; set; } = [];
	[Parameter] public List<ColumnInfo> ColumnInfos { get; set; } = [];
	[Parameter] public List<ColumnSetting> ColumnSettings { get; set; } = [];
	[Parameter] public ITotalsDto? Totals { get; set; }
	[Parameter] public bool ShowTotal { get; set; } = true;
	[Parameter] public string TableKey { get; set; } = "dynamic-table";
	[Parameter] public bool ShowActions { get; set; }
	[Parameter] public RenderFragment? RowActions { get; set; }
	[Parameter] public RenderFragment<TListItem>? RowTemplate { get; set; }
	[Parameter] public bool CalculateTotal { get; set; } = true;
	[Parameter] public string? TotalDistinctBy { get; set; } // Чтобы в итого не попадали одинаковые заказы 
															 // (например, если в таблице один и тот же заказ с разными статусами - история операций)
	[Parameter] public bool AllowSorting { get; set; } = false;
	[Parameter] public EventCallback<(string SortBy, bool Ascending)> OnSortChanged { get; set; }

	private List<ColumnInfo> columnInfos = [];
	private List<ColumnSetting> columnSettings = [];
	private bool isColumnsOpen;
	private string? sortBy;
	private bool sortAscending = true;
	private bool isResizeMode;

	protected override async Task OnInitializedAsync()
	{
		//ShowTotal &= CalculateTotal;

		columnInfos = ColumnHelper.GetColumns<TListItem>();

		columnSettings = ColumnSettings?.Any() == true
			? ColumnSettings
			: TableSettingsManager.GetDefaultSettings<TListItem>();

		await LoadSettings();
		await LoadSortSettings();

	}

	private async Task LoadSettings()
	{
		try
		{
			var tableSettings = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"table_settings_{TableKey}");
			columnSettings = TableSettingsManager.GetSettings<TListItem>(tableSettings);
		}
		catch
		{
			columnSettings = TableSettingsManager.GetDefaultSettings<TListItem>();
		}
	}

	private List<(ColumnInfo Info, ColumnSetting Setting)> BuildVisibleColumns() =>
		columnSettings
		.Where(s => s.Visible)
		.Join(columnInfos, s => s.PropertyName, i => i.PropertyName, (s, i) => (Info: i, Setting: s))
		.OrderBy(x => x.Setting.Order)
		.ToList();

	private async Task ToggleColumn(string propertyName, bool visible)
	{
		var setting = columnSettings.FirstOrDefault(s => s.PropertyName == propertyName);
		if (setting != null) setting.Visible = visible;
		await SaveSettings();
		StateHasChanged();
	}

	private async Task MoveColumn(string propertyName, int direction)
	{
		var setting = columnSettings.FirstOrDefault(s => s.PropertyName == propertyName);
		
		if (setting == null) return;
		
		var ordered = columnSettings.OrderBy(s => s.Order).ToList();

		for (int i = 0; i < ordered.Count; i++)
			ordered[i].Order = i;

		var idx = ordered.IndexOf(setting);
		var newIdx = idx + direction;
		
		if (newIdx < 0 || newIdx >= ordered.Count) return;
		
		(ordered[idx].Order, ordered[newIdx].Order) = (ordered[newIdx].Order, ordered[idx].Order);

		await SaveSettings();
		StateHasChanged();
	}

	private async Task ResetColumns()
	{
		columnSettings = ColumnHelper.GetDefaultSettings<TListItem>();
		await SaveSettings();
		StateHasChanged();
	}

	private async Task SaveSettings()
	{
		var json = TableSettingsManager.Serialize(columnSettings);
		await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"table_settings_{TableKey}", json);
	}

	private IEnumerable<TListItem> ItemsForTotal =>
	string.IsNullOrEmpty(TotalDistinctBy)
		? Items
		: Items
			.GroupBy(i => typeof(TListItem).GetProperty(TotalDistinctBy!)?.GetValue(i))
			.Select(g => g.First());


	private decimal GetColumnTotal(ColumnInfo column)
	{
		if (!column.ShowTotal) return 0;

		return ItemsForTotal.Sum(item =>
		{
			var prop = typeof(TListItem).GetProperty(column.PropertyName);
			var value = prop?.GetValue(item);
			return value switch
			{
				int i => i,
				decimal d => d,
				double d => (decimal)d,
				_ => 0
			};
		});
	}

	private IEnumerable<TListItem> SortedItems
	{
		get
		{
			if (string.IsNullOrEmpty(sortBy)) return Items;

			var prop = typeof(TListItem).GetProperty(sortBy);
			if (prop == null) return Items;

			return sortAscending
				? Items.OrderBy(i => prop.GetValue(i))
				: Items.OrderByDescending(i => prop.GetValue(i));
		}
	}

	private async Task SortBy(string propertyName)
	{
		if (sortBy == propertyName)
		{
			sortAscending = !sortAscending;
		}
		else
		{
			sortBy = propertyName;
			sortAscending = true;
		}

		await SaveSortSettings();

		if (OnSortChanged.HasDelegate)
			await OnSortChanged.InvokeAsync((sortBy, sortAscending));

		StateHasChanged();
	}

	private IconInfo GetSortIconAttributes(ColumnInfo column)
	{
		if (!column.Sortable)
			return new IconInfo();

		if (sortBy != column.PropertyName)
			return new IconInfo
			{
				Class = "bi-arrow-down-up",
				Css = "sort-icon-default",
				Title = "Сортировать"
			};

		return sortAscending
			? new IconInfo
			{
				Class = "bi-sort-down-alt",
				Css = "sort-icon-asc",
				Title = "По возрастанию"
			}
			: new IconInfo
			{
				Class = "bi-sort-down",
				Css = "sort-icon-desc",
				Title = "По убыванию"
			};
	}

	private async Task SaveSortSettings()
	{
		var sortData = JsonSerializer.Serialize(new
		{
			SortBy = sortBy,
			Ascending = sortAscending
		});
		await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"sort_{TableKey}", sortData);
	}

	private async Task LoadSortSettings()
	{
		try
		{
			var json = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"sort_{TableKey}");
			if (!string.IsNullOrEmpty(json))
			{
				var data = JsonSerializer.Deserialize<SortData>(json);
				sortBy = data?.SortBy;
				sortAscending = data?.Ascending ?? true;
			}
		}
		catch { }
	}

	private void ToggleResizeMode()
	{
		isResizeMode = !isResizeMode;
		StateHasChanged();
	}

	private async Task UpdateWidth(string propertyName, string? value)
	{
		if (int.TryParse(value, out var width))
		{
			var setting = columnSettings.FirstOrDefault(s => s.PropertyName == propertyName);
			if (setting != null)
			{
				setting.Width = width;
				// Не сохраняем при каждом движении — только при отпускании
			}
		}
	}

	private async Task SaveWidths()
	{
		await SaveSettings();
		//isResizeMode = false;
		StateHasChanged();
	}

	private async Task ResetWidth(string propertyName)
	{
		var setting = columnSettings.FirstOrDefault(s => s.PropertyName == propertyName);
		if (setting != null)
		{
			setting.Width = 0; // 0 = авто
			await SaveWidths();
		}
	}

	private class IconInfo
	{
		public string Class { get; set; } = "";
		public string Css { get; set; } = "";
		public string Title { get; set; } = "";
	}

	private string GetTotalValue(ColumnInfo column)
	{
		if (Totals == null || !column.ShowTotal)
			return string.Empty;

		// По соглашению: WindowCount → WindowCountTotal
		var totalPropertyName = $"{column.PropertyName}Total";

		var totalProp = Totals.GetType().GetProperty(totalPropertyName,
			BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

		if (totalProp == null)
			return "—";

		var value = totalProp.GetValue(Totals);
		if (value == null)
			return "—";

		// Форматируем
		return value switch
		{
			int i => i.ToString("N0"),
			long l => l.ToString("N0"),
			decimal d => d.ToString(column.DisplayFormat ?? "N2"),
			double dbl => dbl.ToString(column.DisplayFormat ?? "N2"),
			float f => f.ToString(column.DisplayFormat ?? "N2"),
			_ => value.ToString() ?? "—"
		};
	}

}