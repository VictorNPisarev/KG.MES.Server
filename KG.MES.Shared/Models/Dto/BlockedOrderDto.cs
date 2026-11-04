using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;
public class BlockedOrderDto
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("production_order_id")]
	public string ProductionOrderId { get; set; } = string.Empty;

	[JsonPropertyName("order_number")]
	public string OrderNumber { get; set; } = string.Empty;

	[JsonPropertyName("reason")]
	public string? Reason { get; set; }

	[JsonPropertyName("blocked_at")]
	public DateTime BlockedAt { get; set; }

	[JsonPropertyName("user_name")]
	public string? UserName { get; set; }
}
