using KG.MES.Shared.Attributes;
using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.Shared.Models.ViewModels;

public class SupplyViewModel
{
	public Guid Id { get; set; }

	[Column("№ заказа", Order = 0, IconConditions = new[] { "IsClaim:Claim", "IsEconom:Econom" }, Sortable = true)]
	public string OrderNumber { get; set; } = string.Empty;

	[Column("Дата запуска", Visible = false, DisplayFormat = "dd.MM.yyyy", Sortable = true)]
	public DateTime? RtmDate { get; set; }

	[Column("Готовность", Order = 1, DisplayFormat = "dd.MM.yyyy", Sortable = true)]
	public DateTime? ReadyDate { get; set; }

	[Column("Станок", Order = 2, IsBadge = true, Sortable = true, Filterable = true)]
	public string? Machine { get; set; }

	public Guid ProductionOrderId { get; set; }

	public Guid? CurrentWorkplaceId { get; set; }

	[Column("Статус", Order = 3, IsBadge = true, DisplayGroup = "workplace_name")]
	public string? Status { get; set; }

	public bool IsEconom { get; set; }

	public bool IsClaim { get; set; }

	public bool IsOnlyPaid { get; set; }

	public bool IsTwoSidePaint { get; set; }

	[Column("Пиломатериалы", Order = 4, IsBadge = true, DisplayGroup = "supply_status", CommentField = "LumberComment", Filterable = true)]
	public string? Lumber { get; set; }

	[Column("Брус прим.", Order = 5, Visible = false, DisplayGroup = "supply_status")]
	public string? LumberComment { get; set; }

	[Column("ЛКМ", Order = 6, IsBadge = true, DisplayGroup = "supply_status", CommentField = "PaintComment", Filterable = true)]
	public string? Paint { get; set; }

	[Column("ЛКМ прим.", Order = 7, Visible = false, DisplayGroup = "supply_status")]
	public string? PaintComment { get; set; }

	[Column("Стекло", Order = 8, IsBadge = true, DisplayGroup = "supply_status", CommentField = "GlassComment", Filterable = true)]
	public string? Glass { get; set; }

	[Column("Стекло прим.", Order = 9, Visible = false, DisplayGroup = "supply_status")]
	public string? GlassComment { get; set; }

	[Column("Фурнитура", Order = 10, IsBadge = true, DisplayGroup = "supply_status", CommentField = "FurnitureComment", Filterable = true)]
	public string? Furniture { get; set; }

	[Column("Фурнитура прим.", Order = 11, Visible = false, DisplayGroup = "supply_status")]
	public string? FurnitureComment { get; set; }

	[Column("ППС, В/О", Order = 12, IsBadge = true, DisplayGroup = "supply_status", CommentField = "AlumWaterShieldComment", Filterable = true)]
	public string? AlumWaterShield { get; set; }

	[Column("ППС, В/О прим.", Order = 13, Visible = false, DisplayGroup = "supply_status")]
	public string? AlumWaterShieldComment { get; set; }

	[Column("Отлив", Order = 14, IsBadge = true, DisplayGroup = "supply_status", CommentField = "WindowsillComment", Filterable = true)]
	public string? Windowsill { get; set; }

	[Column("Отлив прим.", Order = 15, Visible = false, DisplayGroup = "supply_status")]
	public string? WindowsillComment { get; set; }

	[Column("Д/А", Order = 16, IsBadge = true, DisplayGroup = "supply_status", CommentField = "WoodAlumComment", Filterable = true)]
	public string? WoodAlum { get; set; }

	[Column("Д/А прим.", Order = 17, Visible = false, DisplayGroup = "supply_status")]
	public string? WoodAlumComment { get; set; }


	[Column("***", Order = 2, Visible = false, IconConditions = new[] { "IsClaim:Claim",
																		"IsEconom:Econom",
																		"IsOnlyPaid:Paid",
																		"IsTwoSidePaint:TwoSidePaint",
																		"IsOnlyPlate:Plate" })]
	public string OrderFlags { get; } = string.Empty;

	public SupplyViewModel () {}

	public SupplyViewModel (SupplyDto supplyDto)
	{
		supplyDto.Adapt(this);
	}
}