using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class OrderAttributeDto
{
	[JsonPropertyName("key")]
	public string Key { get; set; } = string.Empty;

	[JsonPropertyName("icon")]
	public string Icon { get; set; } = string.Empty;

	[JsonPropertyName("displayText")]
	public string DisplayText { get; set; } = string.Empty;

	[JsonPropertyName("value")]
	public object? Value { get; set; }
}