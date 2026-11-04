using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class SupplyTypeDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("display_name")]
	public string? DisplayName { get; set; }

	[JsonPropertyName("unit")]
	public string? Unit { get; set; }

	[JsonPropertyName("is_active")]
	public bool IsActive { get; set; }
}

public static class SupplyTypeExtensions
{
	public static string DisplayName(this SupplyTypeDto supplyType) => supplyType.Name switch
	{
		"lumber" => "Брус",
		"furniture" => "Фурнитура",
		"glass" => "Стекло",
		"paint" => "ЛКМ",
		"alumWaterShield" => "ППС, В/О",
		"windowsill" => "Отлив",
		"woodAlum" => "Д/А",
		_ => supplyType.Name
	};
}