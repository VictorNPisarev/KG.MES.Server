using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class WorkplaceTraceDto
{
	[JsonPropertyName("workplaceId")]
	public Guid WorkplaceId { get; set; }

	[JsonPropertyName("workplaceName")]
	public string WorkplaceName { get; set; } = string.Empty;

	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;
}
