using KG.MES.Main.Models;
using KG.MES.Main.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace KG.MES.Main.Pages;

[Authorize(Roles = "Admin")]
public partial class LumberCalculation
{
	[Inject] private KlaesXlsParserService ParserService { get; set; } = null!;
	[Inject] private LumberImportStateService StateService { get; set; } = null!;
	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;
	[Inject] private ILogger<LumberCalculation> Logger { get; set; } = null!;

	private bool isLoading;
	private string activeTab = "summary";
	private string StatusMessage { get; set; } = string.Empty;
	private string AlertClass { get; set; } = "alert-info";
	private List<string> Errors { get; } = [];
	private List<LumberSummaryGroup> LumberSummaryGroups => StateService.GetLumberSummaryGrouped();
	private List<LumberSummaryGroup> BeadSummaryGroups => StateService.GetGlazingBeadSummaryGrouped();

	private List<LumberSummaryItem> SummaryItems => StateService.GetLumberSummary();
	//private List<KlaesLumberRow> BeadRows => StateService.GetGlazingBeadRows();
	private List<LumberSummaryItem> BeadRows => StateService.GetGlazingBeadSummary();
	private List<KlaesLumberRow> AllRows => StateService.Orders.SelectMany(o => o.Rows).ToList();

	private decimal TotalLumberMeters => StateService.Orders.Sum(o => o.TotalLumberMeters);
	private decimal TotalBeadMeters => StateService.Orders.Sum(o => o.TotalBeadMeters);

	private async Task TriggerFileInput()
	{
		await JSRuntime.InvokeVoidAsync("fileUtils.triggerFileInput", "klaesXlsInput");
	}

	private async Task HandleFilesSelected(InputFileChangeEventArgs e)
	{
		var files = e.GetMultipleFiles(maximumFileCount: 100);
		if (files.Count == 0)
			return;

		isLoading = true;
		Errors.Clear();
		StatusMessage = string.Empty;
		StateHasChanged();

		var allAdded = new List<string>();
		var allOverwritten = new List<string>();

		try
		{
			const long maxFileSize = 15 * 1024 * 1024; // 15 MB на файл

			foreach (var file in files)
			{
				try
				{
					if (file.Size > maxFileSize)
					{
						Errors.Add($"{file.Name}: превышен максимальный размер 15 МБ.");
						continue;
					}

					await using var stream = file.OpenReadStream(maxFileSize);
					using var ms = new MemoryStream();
					await stream.CopyToAsync(ms);
					ms.Position = 0;

					var rows = ParserService.Parse(ms, file.Name);
					var (added, overwritten) = StateService.UpsertRows(rows, file.Name);

					allAdded.AddRange(added);
					allOverwritten.AddRange(overwritten);
				}
				catch (Exception ex)
				{
					Logger.LogError(ex, "Ошибка разбора файла {FileName}", file.Name);
					Errors.Add($"{file.Name}: {ex.Message}");
				}
			}

			var parts = new List<string>();
			if (allAdded.Any())
				parts.Add($"добавлены заказы: {string.Join(", ", allAdded.Distinct().Select(x => $"№{x}"))}");
			if (allOverwritten.Any())
				parts.Add($"перезаписаны заказы: {string.Join(", ", allOverwritten.Distinct().Select(x => $"№{x}"))}");

			if (parts.Any())
			{
				StatusMessage = $"Готово — {string.Join("; ", parts)}.";
				AlertClass = "alert-success";
			}
		}
		finally
		{
			isLoading = false;
			StateHasChanged();
		}
	}

	private void RemoveOrder(string orderNumber)
	{
		StateService.RemoveOrder(orderNumber);
	}

	private void ClearAll()
	{
		StateService.Clear();
		Errors.Clear();
		StatusMessage = string.Empty;
	}
}
