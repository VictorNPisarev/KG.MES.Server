using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class SupplyOrderListItemDto
{
	[JsonPropertyName("order_supply_id")]
	public Guid OrderSupplyId { get; set; }

	[JsonPropertyName("supply_type_id")]
	public Guid SupplyTypeId { get; set; }

	[JsonPropertyName("supply_condition_id")]
	public Guid? SupplyConditionId { get; set; }

	[JsonPropertyName("expected_date")]
	public DateTime? ExpectedDate { get; set; }

	[JsonPropertyName("quantity")]
	public decimal? Quantity { get; set; }

	[JsonPropertyName("comment_id")]
	public Guid? CommentId { get; set; }

	[JsonPropertyName("comment")]
	public string? Comment { get; set; }
}