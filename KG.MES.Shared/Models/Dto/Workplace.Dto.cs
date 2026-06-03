using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class WorkplaceDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("is_workplace")]
	public bool IsWorkplace { get; set; }
}

public class WorkplaceBlockDto
{
	public Guid Id { get; set; }
	public Guid ProductionOrderId { get; set; }
	public string OrderNumber { get; set; } = string.Empty;
	public string? Reason { get; set; }
	public DateTime BlockedAt { get; set; }
	public string? UserName { get; set; }
}