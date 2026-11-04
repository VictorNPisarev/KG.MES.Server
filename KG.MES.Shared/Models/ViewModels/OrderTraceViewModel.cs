using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.Shared.Models.ViewModels;

public class OrderTraceViewModel
{
	public Guid OrderId { get; set; }

	public Guid ProductionOrderId { get; set; }

	public string OrderNumber { get; set; } = string.Empty;

	public DateTime? ReadyDate { get; set; }

	public bool? Departed { get; set; }

	public List<WorkplaceTraceViewModel> WorkplaceTraces { get; set; } = [];

	public OrderTraceViewModel () {}

	public OrderTraceViewModel (OrderTraceDto orderTraceDto)
	{
		orderTraceDto.Adapt(this);

		WorkplaceTraces = orderTraceDto.WorkplaceTraces.Select(w => w.Adapt<WorkplaceTraceViewModel>()).ToList();
	}
}