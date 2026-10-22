using KG.MES.Shared.Attributes;
using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.Shared.Models.ViewModels;

public class MastersOrderViewModel
{
	public Guid Id { get; set; }

	[Column("№ заказа", Order = 1, IconConditions = new[] { "IsClaim:Claim", "IsOnlyPlate:Plate", "IsEconom:Econom", "IsTwoSidePaint:TwoSidePaint" }, Sortable = true)]
	public string OrderNumber { get; set; } = string.Empty;

	[Column("Статус", Order = 3, IsBadge = true, DisplayGroup = "workplace_name", Sortable = true)]
	public string? CurrentStatus { get; set; }

	[Column("Дата запуска", Order = 4, DisplayFormat = "dd.MM.yyyy", Sortable = true)]
	public DateTime? RtmDate { get; set; }

	[Column("Готовность", Order = 5, DisplayFormat = "dd.MM.yyyy", Sortable = true)]
	public DateTime? ReadyDate { get; set; }

	[Column("Окна, шт", Order = 6)]
	public int WindowCount { get; set; }

	[Column("Окна, м2", Order = 7, DisplayFormat = "F2")]
	public double? WindowArea { get; set; }

	[Column("Щитовые, шт", Order = 8)]
	public int PlateCount { get; set; }

	[Column("Щитовые, м2", Order = 9, DisplayFormat = "F2")]
	public double? PlateArea { get; set; }

	[Column("Эконом", Order = 10, IsBadge = true, Filterable = true)]
	public bool IsEconom { get; set; }

	[Column("Рекламация", Order = 11, IsBadge = true, Filterable = true)]
	public bool IsClaim { get; set; }

	[Column("Оплачен, не запущен", Order = 12, IsBadge = true, Filterable = true)]
	public bool IsOnlyPaid { get; set; }

	[Column("2-стор. покраска", Order = 13, IsBadge = true, Filterable = true)]
	public bool IsTwoSidePaint { get; set; }

	public string? ProductionOrderId { get; set; }

	public string? CurrentWorkplaceId { get; set; }

	[Column("Контрагент", Visible = false)]
	public string CustomerName { get; set; } = string.Empty;

	public string? CurrentWorkplaceName { get; set; }

	[Column("Станок", Order = 12, Visible = true, IsBadge = true, Sortable = true, Filterable = true)]
	public string? Machine { get; set; }

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

	public MastersOrderViewModel () {}

	public MastersOrderViewModel (MastersOrderDto mastersOrderDto)
	{
		mastersOrderDto.Adapt(this);
	}
}