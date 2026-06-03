using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class SupplyTypeDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("display_name")]
	public string DisplayName { get; set; } = string.Empty;

	[JsonPropertyName("unit")]
	public string? Unit { get; set; }

	[JsonPropertyName("sort_order")]
	public int SortOrder { get; set; }

	[JsonPropertyName("is_active")]
	public bool IsActive { get; set; }
}