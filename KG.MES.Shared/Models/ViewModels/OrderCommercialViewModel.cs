using KG.MES.Shared.Attributes;
using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.Shared.Models.ViewModels;

public class OrderCommercialViewModel
{
	public Guid Id { get; set; }

	public Guid OrderId { get; set; }

	public Guid? ManagerId { get; set; }

	[Column("Менеджер", Order = 0, Filterable = true)]
	public string? ManagerName { get; set; }

	public Guid? CustomerId { get; set; }

	[Column("Контрагент", Order = 1, Filterable = true)]
	public string? CustomerName { get; set; }

	[Column("Цена", Order = 2, DisplayFormat = "C")]
	public decimal? Amount { get; set; }

	[Column("Цена", Order = 2, Visible = false)]
	public string? Currency { get; set; }

	public OrderCommercialViewModel () {}

	public OrderCommercialViewModel (OrderCommercialDto orderCommercialDto)
	{
		orderCommercialDto.Adapt(this);
	}
}
