namespace KG.MES.Server.Models.Dto;

public class UpdateFootprintBatchRequest
{
	public List<FootprintItemDto> Footprints { get; set; } = new();
	public Guid? UserId { get; set; }
	public string? Notes { get; set; }
}