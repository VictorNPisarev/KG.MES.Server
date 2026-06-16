using System.Text.Json.Serialization;

namespace KG.MES.Server.Models.Dto;

public class SetFootprintStatusRequestDto
{
	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;

	[JsonPropertyName("userId")]
	public Guid? UserId { get; set; }

	[JsonPropertyName("notes")]
	public string? Notes { get; set; }
}
