namespace KG.MES.Shared.Models.Dto;

public class SetOrderStatusResultDto
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public string? OldStatus { get; set; }
}