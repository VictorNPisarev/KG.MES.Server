using KG.MES.Shared.Attributes;
using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.Shared.Models.ViewModels;

public class WorkplaceHistoryViewModel
{
	[Column("Время", Order = 4, DisplayFormat = "dd.MM.yyyy HH:mm", Sortable = true)]
	public DateTime OperationTime { get; set; }

	[Column("Тип", Order = 0, IsBadge = true, BadgeGroup = "workplace_status", Sortable = true,
		IconConditions = new[] { "IsStartTransfer:StartOperationTransfer", "IsCompleteTransfer:CompleteOperationTransfer" })]
	public string OperationType { get; set; } = string.Empty;

	[Column("Заказ", Order = 2, Sortable = true)]
	public string OrderNumber { get; set; } = string.Empty;

	[Column("Готовность", Order = 3, DisplayFormat = "dd.MM.yyyy", Sortable = true)]
	public DateTime? ReadyDate { get; set; }

	[Column("Сотрудник", Order = 5, Sortable = true)]
	public string? UserName { get; set; }

	[Column("Примечание", Order = 6)]
	public string? Notes { get; set; }

	[Column("Окна, шт", Order = 7, ShowTotal = true)]
	public int WindowCount { get; set; }

	[Column("Окна, м²", Order = 8, DisplayFormat = "F2", ShowTotal = true)]
	public decimal? WindowArea { get; set; }

	[Column("Щитовые, шт", Order = 9, ShowTotal = true)]
	public int PlateCount { get; set; }

	[Column("Щитовые, м²", Order = 10, DisplayFormat = "F2", ShowTotal = true)]
	public decimal? PlateArea { get; set; }

	[Column("Эконом", Order = 11, IsBadge = true)]
	public bool IsEconom { get; set; }

	[Column("Рекламация", Order = 12, IsBadge = true)]
	public bool IsClaim { get; set; }

	[Column("Оплачен, не запущен", Order = 13, IsBadge = true)]
	public bool IsOnlyPaid { get; set; }

	[Column("2-стор. покраска", Order = 14, IsBadge = true)]
	public bool IsTwoSidePaint { get; set; }

	[Column("***", Order = 1, Visible = false, IconConditions = new[] { "IsClaim:Claim",
																		"IsEconom:Econom",
																		"IsOnlyPaid:Paid",
																		"IsTwoSidePaint:TwoSidePaint",
																		"IsOnlyPlate:Plate" })]
	public string OrderFlags { get; } = string.Empty;

	public bool IsOnlyPlate
	{
		get
		{
			return WindowCount == 0 && PlateCount > 0;
		}
	}

	public bool? IsStartTransfer { get; set; }

	public bool? IsCompleteTransfer { get; set; }

	/// <summary>
	/// Индикатор переходящих заказов. Если завершенный заказ был взят в работу раньше выбранного периода, то возвращаю -1.
	/// Заказ взятый в работу, но не завершенный или завершенный позже выбранного периода - возвращяю 1.
	/// Если в выбранный период заказ прошел полный цикл (взят в работу и закончен) - 0
	/// </summary>
	/// <param name="workplaceOperation"></param>
	/// <param name="workplaceHistory"></param>
	/// <param name="startPeriod"></param>
	/// <param name="endPeriod"></param>
	/// <returns></returns>
	public int PeriodTransferIndicator(List<WorkplaceHistoryViewModel> workplaceHistory,
												DateTime startPeriod, DateTime endPeriod)
	{
		if(OperationType == Constants.OrderStatus.OperationType.Start)
		{
			var secondOperation = workplaceHistory.FirstOrDefault(o => o.OrderNumber == OrderNumber 
															&& o.OperationType == Constants.OrderStatus.OperationType.Complete);

			if(secondOperation == null || secondOperation.OperationTime > endPeriod)
				return 1;
			else
				return 0;
		}

		if (OperationType == Constants.OrderStatus.OperationType.Complete)
		{
			var secondOperation = workplaceHistory.FirstOrDefault(o => o.OrderNumber == OrderNumber
															&& o.OperationType == Constants.OrderStatus.OperationType.Start);

			if (secondOperation == null || secondOperation.OperationTime < startPeriod)
				return -1;
			else
				return 0;
		}

		return 0;
	}

	public WorkplaceHistoryViewModel() { }

	public WorkplaceHistoryViewModel(WorkplaceHistoryDto dto)
	{
		dto.Adapt(this);
	}
}