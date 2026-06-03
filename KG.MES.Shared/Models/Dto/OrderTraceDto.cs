namespace KG.MES.Shared.Models.Dto;

public class OrderTraceDto
{
	public Guid OrderId { get; set; }
	public Guid? ProductionOrderId { get; set; }
	public string OrderNumber { get; set; } = string.Empty;
	public DateTime? ReadyDate { get; set; }
	public string Status { get; set; } = string.Empty;
	public List<WorkplaceTraceDto> Workplaces { get; set; } = new();
}

public class WorkplaceTraceDto
{
	public Guid WorkplaceId { get; set; }
	public string WorkplaceName { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
}