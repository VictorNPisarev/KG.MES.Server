namespace KG.MES.Shared.Models.Dto;

public class CreateOrderResultDto
{
	public bool Success { get; set; }
	public Guid OrderId { get; set; }
	public Guid ProductionOrderId { get; set; }
	public Guid? OrderSupplyId { get; set; }
	public List<Guid> SupplyItemIds { get; set; } = new();
}