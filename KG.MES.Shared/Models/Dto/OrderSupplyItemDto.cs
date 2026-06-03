using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class OrderSupplyItemDto
{
	[JsonPropertyName("order_supply_id")]
	public Guid OrderSupplyId { get; set; }

	[JsonPropertyName("supply_type_id")]
	public Guid SupplyTypeId { get; set; }

	[JsonPropertyName("supply_type_name")]
	public string SupplyTypeName { get; set; } = string.Empty;

	[JsonPropertyName("display_name")]
	public string DisplayName { get; set; } = string.Empty;

	[JsonPropertyName("unit")]
	public string? Unit { get; set; }

	[JsonPropertyName("supply_condition_id")]
	public Guid? SupplyConditionId { get; set; }

	[JsonPropertyName("condition_code")]
	public string? ConditionCode { get; set; }

	[JsonPropertyName("condition_display_name")]
	public string? ConditionDisplayName { get; set; }

	[JsonPropertyName("expected_date")]
	public DateTime? ExpectedDate { get; set; }

	[JsonPropertyName("quantity")]
	public decimal? Quantity { get; set; }

	[JsonPropertyName("comment")]
	public string? Comment { get; set; }
}