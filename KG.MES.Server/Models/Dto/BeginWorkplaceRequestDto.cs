namespace KG.MES.Server.Models.Dto;

public class BeginWorkplaceRequestDto
{
	public Guid ProductionOrderId { get; set; }
	public Guid WorkplaceId { get; set; }
	public Guid UserId { get; set; }
	public string? Notes { get; set; }
	public string? Source { get; set; }
}