using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class SupplyItemHistoryDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("supply_type_name")]
	public string? SupplyTypeName { get; set; }

	[JsonPropertyName("condition_code")]
	public string? ConditionCode { get; set; }

	[JsonPropertyName("comment")]
	public string? Comment { get; set; }

	[JsonPropertyName("user_id")]
	public Guid? UserId { get; set; }

	[JsonPropertyName("user_name")]
	public string? UserName { get; set; }

	[JsonPropertyName("operation_time")]
	public DateTime OperationTime { get; set; }

	[JsonPropertyName("source")]
	public string? Source { get; set; }
}

public static class SupplyHistoryItemExtensions
{
	public static string DisplayName(this SupplyItemHistoryDto supplyHistoryItem) => supplyHistoryItem.SupplyTypeName switch
	{
		"lumber" => "Брус",
		"furniture" => "Фурнитура",
		"glass" => "Стекло",
		"paint" => "ЛКМ",
		"alumWaterShield" => "ППС, В/О",
		"windowsill" => "Отлив",
		"woodAlum" => "Д/А",
		_ => supplyHistoryItem.SupplyTypeName
	};
}
