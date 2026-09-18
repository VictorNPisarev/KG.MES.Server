using KG.MES.Shared.Attributes;
using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.Shared.Models.ViewModels;

public class CommerceOrderViewModel
{
	public Guid Id { get; set; }

	public Guid OrderId { get; set; }

	[Column("Менеджер", Order = 0)]
	public string? Manager { get; set; }

	[Column("Контрагент", Order = 1)]
	public string? CustomerName { get; set; }

	[Column("Цена", Order = 2, DisplayFormat = "C")]
	public decimal Price { get; set; }

	public DateTime CreatedAt { get; set; }

	public DateTime? UpdatedAt { get; set; }

	public CommerceOrderViewModel () {}

	public CommerceOrderViewModel (CommerceOrderDto commerceOrderDto)
	{
		commerceOrderDto.Adapt(this);
	}
}
