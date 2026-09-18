using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class MastersOrderDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("order_number")]
	public string OrderNumber { get; set; } = string.Empty;

	[JsonPropertyName("current_status")]
	public string? CurrentStatus { get; set; }

	[JsonPropertyName("rtm_date")]
	public DateTime? RtmDate { get; set; }

	[JsonPropertyName("ready_date")]
	public DateTime? ReadyDate { get; set; }

	[JsonPropertyName("window_count")]
	public int WindowCount { get; set; }

	[JsonPropertyName("window_area")]
	public double? WindowArea { get; set; }

	[JsonPropertyName("plate_count")]
	public int PlateCount { get; set; }

	[JsonPropertyName("plate_area")]
	public double? PlateArea { get; set; }

	[JsonPropertyName("is_econom")]
	public bool IsEconom { get; set; }

	[JsonPropertyName("is_claim")]
	public bool IsClaim { get; set; }

	[JsonPropertyName("is_only_paid")]
	public bool IsOnlyPaid { get; set; }

	[JsonPropertyName("is_two_side_paint")]
	public bool IsTwoSidePaint { get; set; }

	[JsonPropertyName("production_order_id")]
	public string? ProductionOrderId { get; set; }

	[JsonPropertyName("current_workplace_id")]
	public string? CurrentWorkplaceId { get; set; }

	[JsonPropertyName("customer_name")]
	public string CustomerName { get; set; } = string.Empty;

	[JsonPropertyName("current_workplace_name")]
	public string? CurrentWorkplaceName { get; set; }

	[JsonPropertyName("machine")]
	public string? Machine { get; set; }
}