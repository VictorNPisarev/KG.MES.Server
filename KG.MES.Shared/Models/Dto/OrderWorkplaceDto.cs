using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class OrderWorkplaceDto
{
	[JsonPropertyName("id")]
	public Guid ProductionOrderId { get; set; } // Это будет productionOrderId

	[JsonPropertyName("workplace_id")]
	public Guid WorkplaceId { get; set; }

	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;

	[JsonPropertyName("order_id")]
	public Guid OrderId { get; set; }

	[JsonPropertyName("order_number")]
	public string OrderNumber { get; set; } = string.Empty;

	[JsonPropertyName("window_count")]
	public int WindowCount { get; set; }

	[JsonPropertyName("window_area")]
	public decimal? WindowArea { get; set; }

	[JsonPropertyName("plate_count")]
	public int PlateCount { get; set; }

	[JsonPropertyName("plate_area")]
	public decimal? PlateArea { get; set; }

	[JsonPropertyName("ready_date")]
	public DateTime? ReadyDate { get; set; }

	[JsonPropertyName("is_econom")]
	public bool IsEconom { get; set; }

	[JsonPropertyName("is_claim")]
	public bool IsClaim { get; set; }

	[JsonPropertyName("is_only_paid")]
	public bool IsOnlyPaid { get; set; }

	[JsonPropertyName("workplaceOrderStatus")]
	public string WorkplaceOrderStatus { get; set; } = string.Empty;

	[JsonPropertyName("fromJoinery")]
	public bool FromJoinery { get; set; }

	[JsonPropertyName("Name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("isBlocked")]
	public bool IsBlocked { get; set; }

	[JsonPropertyName("blocks")]
	public List<OrderBlockDto> Blocks { get; set; } = new();

	[JsonPropertyName("attributes")]
	public List<OrderAttributeDto> Attributes { get; set; } = new();

}