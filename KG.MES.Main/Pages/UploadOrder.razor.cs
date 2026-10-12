using KG.MES.Main.Extensions;
using KG.MES.Main.Models;
using KG.MES.Main.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using KG.MES.Main.Components;
using KG.MES.Main.Models.Enums;
using KG.MES.Main.Services;
using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace KG.MES.Main.Pages;

public partial class UploadOrder
{
	[Inject] private ProductionApiService ApiService { get; set; } = null!;
	[Inject] private IXmlReaderService XmlReaderService { get; set; } = null!;
	[Inject] private I1CExportService ExportService { get; set; } = null!;
	[Inject] private SortingIndicatorService SortingService { get; set; } = null!;
	[Inject] private HolidayCalendarService HolidayService { get; set; } = null!;
	[Inject] private IDocumentItemFactory ItemFactory { get; set; } = null!;
	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;
	[Inject] private ILogger<Index> Logger { get; set; } = null!;

	private List<DocumentData> Orders { get; set; } = new();
	private DocumentData? SelectedOrder { get; set; }
	private string StatusMessage { get; set; } = "Готов к работе";
	private bool isLoading = false;
	private string statusType = "info";

	private bool HasOrders => Orders.Any();
	private decimal TotalAmount => Orders.Sum(o => o.DocumentAmountGross);
	private int TotalItems => Orders.Sum(o => o.DocumentItems?.Count ?? 0);
	private decimal AverageOrderAmount => HasOrders ? TotalAmount / Orders.Count : 0;

	private async Task TriggerFileInput()
	{
		await JSRuntime.InvokeVoidAsync("fileUtils.triggerFileInput", "fileInputHidden");
	}

	private async Task HandleFileSelected(InputFileChangeEventArgs e)
	{
		try
		{
			isLoading = true;
			StatusMessage = "Чтение XML файла...";
			statusType = "info";
			StateHasChanged();

			if (e.File != null)
			{
				const long maxFileSize = 10 * 1024 * 1024; // 10 MB

				if (e.File.Size > maxFileSize)
				{
					StatusMessage = $"Файл слишком большой. Максимум: {maxFileSize / 1024 / 1024}MB";
					statusType = "warning";
					return;
				}

				using var stream = e.File.OpenReadStream(maxFileSize);
				using var reader = new StreamReader(stream);
				var xmlContent = await reader.ReadToEndAsync();

				var xmlOrder = await XmlReaderService.ReadXmlFromContent(xmlContent);
				var order = new DocumentData(xmlOrder, ItemFactory);

				Orders.Add(order);
				SelectedOrder = order;

				StatusMessage = $"✓ Загружен: {order.DocumentNumber}";
				statusType = "success";
				Logger.LogInformation("Loaded order: {OrderNumber}", order.DocumentNumber);
			}
		}
		catch (Exception ex)
		{
			StatusMessage = $"✗ Ошибка: {ex.Message}";
			statusType = "danger";
			Logger.LogError(ex, "Error loading XML");
		}
		finally
		{
			isLoading = false;
			StateHasChanged();
		}
	}

	private bool isSendingToProduction = false;

	private async Task SendSelectedToProduction()
	{
		if (SelectedOrder == null) return;

		// 1. Показываем, что процесс начался
		isSendingToProduction = true;
		StatusMessage = "📤 Отправка заказа в производство...";
		statusType = "info";
		StateHasChanged(); // Сразу обновляем UI

		try
		{
			var editOrder = SelectedOrder.ToProductionOrderDto(SortingService);

			// Расчёт даты готовности
			editOrder.ReadyDate = HolidayService.AddBusinessDays(SelectedOrder.InitializeDate, SelectedOrder.ApprovedLeadTimeDays);

			var success = await ApiService.ExportToProductionAsync(editOrder);

			if (success)
			{
				StatusMessage = $"✅ Заказ {SelectedOrder.DocumentNumber} успешно передан в производство!";
				statusType = "success";

				// Автоочистка через 5 сек (опционально)
				_ = Task.Delay(5000).ContinueWith(_ =>
				{
					if (statusType == "success") StatusMessage = "";
					StateHasChanged();
				});
			}
			else
			{
				StatusMessage = "❌ Не удалось отправить заказ. Проверьте логи сервера.";
				statusType = "danger";
			}
		}
		catch (Exception ex)
		{
			StatusMessage = $"❌ Ошибка: {ex.Message}";
			statusType = "danger";
			Logger.LogError(ex, "Ошибка экспорта в производство");
		}
		finally
		{
			isSendingToProduction = false;
			StateHasChanged();
		}
	}

	private void SelectOrder(DocumentData order)
	{
		SelectedOrder = order;
		StateHasChanged();
	}

	private void ClearStatus()
	{
		StatusMessage = string.Empty;
		StateHasChanged();
	}

	private string GetAlertClass() => statusType switch
	{
		"success" => "alert-success",
		"warning" => "alert-warning",
		"danger" => "alert-danger",
		_ => "alert-info"
	};
}