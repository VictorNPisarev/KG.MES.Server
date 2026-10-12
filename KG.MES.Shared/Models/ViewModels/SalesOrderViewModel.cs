using KG.MES.Shared.Attributes;
using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.Shared.Models.ViewModels;

public class SalesOrderViewModel
{
	public Guid Id { get; set; }

	[Column("№ заказа", Order = 1, IconConditions = new[] { "IsClaim:Claim", "IsEconom:Econom" }, Sortable = true)]
	public string OrderNumber { get; set; } = string.Empty;

	[Column("Дата готовности", Order = 2, DisplayFormat = "dd.MM.yyyy", Sortable = true)]
	public DateTime? ReadyDate { get; set; }

	[Column("Окна, шт", Order = 6)]
	public int WindowCount { get; set; }

	[Column("Окна, м2", Order = 7, DisplayFormat = "F2")]
	public double? WindowArea { get; set; }

	[Column("Щитовые, шт", Order = 8)]
	public int PlateCount { get; set; }

	[Column("Щитовые, м2", Order = 9, DisplayFormat = "F2")]
	public double? PlateArea { get; set; }

	[Column("Эконом", Order = 10, IsBadge = true)]
	public bool IsEconom { get; set; }

	[Column("Рекламация", Order = 11, IsBadge = true)]
	public bool IsClaim { get; set; }

	[Column("Оплачен, не запущен", Order = 12, IsBadge = true)]
	public bool IsOnlyPaid { get; set; }

	[Column("Дата запуска", Visible = false, DisplayFormat = "dd.MM.yyyy", Sortable = true)]
	public DateTime StartDate { get; set; }

	public Guid? ProductionOrderId { get; set; }

	public Guid? CurrentWorkplaceId { get; set; }

	[Column("Статус", Order = 3, IsBadge = true, DisplayGroup = "workplace_name")]
	public string? CurrentStatus { get; set; }

	public Guid? ManagerId { get; set; }

	[Column("Менеджер", Order = 4, Sortable = true)]
	public string? ManagerName { get; set; }

	public Guid? CustomerId { get; set; }

	[Column("Контрагент", Order = 5, Sortable = true)]
	public string? CustomerName { get; set; }

	[Column("Стоимость", Order = 6)]
	public decimal? Amount { get; set; }

	public string? Currency { get; set; }

	[Column("***", Order = 2, Visible = false, IconConditions = new[] { "IsClaim:Claim",
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

	public SalesOrderViewModel () {}

	public SalesOrderViewModel (SalesOrderDto salesOrderDto)
	{
		salesOrderDto.Adapt(this);
	}
}