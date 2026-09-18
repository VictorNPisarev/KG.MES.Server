// CreateOrderPage.razor.cs
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace KG.MES.Main.Pages;

public partial class CreateOrderPage
{
	[SupplyParameterFromQuery(Name = "edit")]
	public string? EditOrderId { get; set; }

	private bool isEditing => !string.IsNullOrEmpty(EditOrderId);

	private string OrderNumber { get; set; } = string.Empty;
	private int WindowCount { get; set; }
	private double WindowArea { get; set; }
	private int PlateCount { get; set; }
	private double PlateArea { get; set; }
	private string Machine { get; set; } = string.Empty;
	private DateTime? RtmDate { get; set; } = DateTime.Now;
	private DateTime? RtmDateCash { get; set; } = DateTime.Now;
	private int ApprovedDays { get; set; }
	private int ApprovedDaysCash { get; set; }
	private int UnapprovedDays { get; set; }
	private int UnapprovedDaysCash { get; set; }
	private DateTime? So8Date { get; set; }
	private string Comment { get; set; } = string.Empty;
	private bool IsEconom { get; set; }
	private bool IsClaim { get; set; }
	private bool IsOnlyPaid { get; set; }
	private bool IsTwoSidePaint { get; set; }

	[Inject] private ProductionApiService ApiService { get; set; } = null!;
	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;
	[Inject] private NavigationManager NavManager { get; set; } = null!;

	private DateTime? ReadyDate { get; set; }
	private bool isSaving;
	private string StatusMessage { get; set; } = string.Empty;
	private bool isError;
	private bool isCalculatingReadyDate;

	protected override async Task OnInitializedAsync()
	{
		if (isEditing && Guid.TryParse(EditOrderId, out var id))
		{
			await LoadOrderForEdit(id);
		}
	}

	private async Task LoadOrderForEdit(Guid orderId)
	{
		var order = await ApiService.GetOrderForEditAsync(orderId);

		if (order != null)
		{
			OrderNumber = order.OrderNumber;
			WindowCount = order.WindowCount;
			WindowArea = order.WindowArea;
			PlateCount = order.PlateCount;
			PlateArea = order.PlateArea;
			Machine = order.Machine ?? "";
			RtmDate = order.RtmDate;
			RtmDateCash = order.RtmDate;
			ApprovedDays = order.ApprovedLeadDays;
			UnapprovedDays = order.UnapprovedLeadDays;
			So8Date = order.So8Date;
			Comment = order.Comment ?? "";
			IsEconom = order.IsEconom;
			IsClaim = order.IsClaim;
			IsOnlyPaid = order.IsOnlyPaid;
			IsTwoSidePaint = order.IsTwoSidePaint;
			ReadyDate = order.ReadyDate;
		}

		StateHasChanged();
	}

	/// <summary>
	/// Вызывается при изменении даты начала или количества дней.
	/// </summary>
	private async Task OnDaysChanged(string elementId, KeyboardEventArgs? e = null)
	{
		if (e != null && e.Key != "Enter") return;

		if (RtmDate != RtmDateCash || ApprovedDays != ApprovedDaysCash || UnapprovedDays != UnapprovedDaysCash)
		{
			await CalculateReadyDateAsync();
			RtmDateCash = RtmDate;
			ApprovedDaysCash = ApprovedDays;
			UnapprovedDaysCash = UnapprovedDays;
		}
	}

	private async Task SaveOrder()
	{
		if (string.IsNullOrWhiteSpace(OrderNumber))
		{
			await ShowStatusMessage("Введите номер заказа", true);
			return;
		}

		isSaving = true;
		StatusMessage = "";

		try
		{
			var dto = new ProductionOrderExportDto
			{
				OrderNumber = OrderNumber,
				WindowCount = WindowCount,
				WindowArea = WindowArea,
				PlateCount = PlateCount,
				PlateArea = PlateArea,
				Comment = Comment,
				IsEconom = IsEconom,
				IsClaim = IsClaim,
				IsOnlyPaid = IsOnlyPaid,
				IsTwoSidePaint = IsTwoSidePaint,
				RtmDate = RtmDate ?? DateTime.Now,
				ApprovedLeadDays = ApprovedDays, // > 0 ? ApprovedDays : UnapprovedDays,
				UnapprovedLeadDays = UnapprovedDays,
				ReadyDate = ReadyDate,
				So8Date = So8Date,
				Machine = Machine
			};

			bool success;
			if (isEditing)
			{
				success = await ApiService.UpdateOrderAsync(Guid.Parse(EditOrderId!), dto);
				if (success) await ShowStatusMessage($"Заказ №{OrderNumber} обновлён");
			}
			else
			{
				success = await ApiService.ExportToProductionAsync(dto);
				if (success)
				{
					await ShowStatusMessage($"Заказ №{OrderNumber} создан", secs: 3);
					ClearForm();
				}
			}

			if (!success)
			{
				await ShowStatusMessage("Ошибка при сохранении", true, 7);
				isError = true;
			}
		}
		catch (Exception ex)
		{
			await ShowStatusMessage($"Ошибка: {ex.Message}", true, 7);
		}
		finally
		{
			isSaving = false;
		}
	}

	private void ClearForm()
	{
		OrderNumber = string.Empty;
		WindowCount = 0;
		WindowArea = 0;
		PlateCount = 0;
		PlateArea = 0;
		Comment = string.Empty;
		IsEconom = false;
		IsClaim = false;
		IsOnlyPaid = false;
		IsTwoSidePaint = false;
		ReadyDate = null;
		ApprovedDays = 0;
		UnapprovedDays = 0;
		So8Date = null;
		Machine = string.Empty;
		RtmDate = DateTime.Now;
		isError = false;
	}

	/// <summary>
	/// Рассчитывает дату готовности через API с учетом производственного календаря.
	/// </summary>
	private async Task CalculateReadyDateAsync()
	{
		var days = ApprovedDays > 0 ? ApprovedDays : UnapprovedDays;

		if (days <= 0)
		{
			ReadyDate = null;
			return;
		}

		isCalculatingReadyDate = true;
		StateHasChanged();

		try
		{
			ReadyDate = await ApiService.CalculateReadyDateAsync(RtmDate ?? DateTime.Now, days);
			StatusMessage = "";
			isError = false;
		}
		catch (Exception ex)
		{
			StatusMessage = $"Ошибка расчета даты: {ex.Message}";
			isError = true;
			ReadyDate = null;
		}
		finally
		{
			isCalculatingReadyDate = false;
			StateHasChanged();
		}
	}

	private async Task ShowStatusMessage(string message, bool error = false, int secs = 4)
	{
		StatusMessage = message;
		isError = error;
		StateHasChanged();

		await Task.Delay(secs * 1000);
		StatusMessage = "";
		StateHasChanged();
	}

	private void HandleDateChange(ChangeEventArgs e, string elementId)
	{
		if (DateTime.TryParse(e.Value?.ToString(), out var date))
		{
			RtmDate = date;
			_ = OnDaysChanged(elementId);
		}
	}
}
