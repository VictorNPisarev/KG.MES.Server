namespace KG.MES.Shared.Models.Dto;

public class BatchUpdateResultDto
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public List<BatchUpdateDetail> Details { get; set; } = new();
}

public class BatchUpdateDetail
{
	public Guid WorkplaceId { get; set; }
	public bool Success { get; set; }
	public string? OldStatus { get; set; }
	public string? NewStatus { get; set; }
	public string? Error { get; set; }
}