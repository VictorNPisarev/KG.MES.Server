using KG.MES.Shared.Attributes;
using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.Shared.Models.ViewModels;

public class WorkplaceOrderViewModel
{
	public Guid ProductionOrderId { get; set; } // Это будет productionOrderId

	public Guid WorkplaceId { get; set; }

	[Column("Статус", Order = 3, IsBadge = true, DisplayGroup = "workplace_name", Sortable = true)]
	public string Status { get; set; } = string.Empty;

	public Guid OrderId { get; set; }

	[Column("№ заказа", Order = 1, IconConditions = new[] { "IsClaim:Claim", "IsEconom:Econom" }, Sortable = true)]
	public string OrderNumber { get; set; } = string.Empty;

	[Column("Окна, шт", Order = 6, ShowTotal = true)]
	public int WindowCount { get; set; }

	[Column("Окна, м2", Order = 7, DisplayFormat = "F2", ShowTotal = true)]
	public decimal? WindowArea { get; set; }

	[Column("Щитовые, шт", Order = 8, ShowTotal = true)]
	public int PlateCount { get; set; }

	[Column("Щитовые, м2", Order = 9, DisplayFormat = "F2", ShowTotal = true)]
	public decimal? PlateArea { get; set; }

	[Column("Готовность", Order = 5, DisplayFormat = "dd.MM.yyyy", Sortable = true)]
	public DateTime? ReadyDate { get; set; }

	[Column("Эконом", Order = 10, IsBadge = true)]
	public bool IsEconom { get; set; }

	[Column("Рекламация", Order = 11, IsBadge = true)]
	public bool IsClaim { get; set; }

	[Column("Оплачен, не запущен", Order = 12, IsBadge = true)]
	public bool IsOnlyPaid { get; set; }

	public string WorkplaceOrderStatus { get; set; } = string.Empty;

	public bool FromJoinery { get; set; }

	public string Name { get; set; } = string.Empty;

	public bool IsBlocked { get; set; }

	public List<OrderBlockDto> Blocks { get; set; } = [];

	public List<OrderAttributeDto> Attributes { get; set; } = [];

	public WorkplaceOrderViewModel () {}

	public WorkplaceOrderViewModel (WorkplaceOrderDto workplaceOrderDto)
	{
		workplaceOrderDto.Adapt(this);
	}
}