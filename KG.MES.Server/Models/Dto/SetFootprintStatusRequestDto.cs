namespace KG.MES.Server.Models.Dto;

public class SetFootprintStatusRequestDto
{
	public string Status { get; set; } = string.Empty;
	public Guid? UserId { get; set; }
	public string? Notes { get; set; }
}
