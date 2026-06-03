using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class SupplyConditionDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("condition_code")]
	public string ConditionCode { get; set; } = string.Empty;

	[JsonPropertyName("display_name")]
	public string DisplayName { get; set; } = string.Empty;

	[JsonPropertyName("sort_order")]
	public int SortOrder { get; set; }
}