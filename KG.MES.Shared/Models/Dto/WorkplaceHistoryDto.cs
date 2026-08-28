using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class WorkplaceHistoryDto
{
	[JsonPropertyName("operation_time")]
	public DateTime OperationTime { get; set; }

	[JsonPropertyName("operation_type")]
	public string OperationType { get; set; } = string.Empty;

	[JsonPropertyName("order_number")]
	public string OrderNumber { get; set; } = string.Empty;

	[JsonPropertyName("user_name")]
	public string? UserName { get; set; }

	[JsonPropertyName("notes")]
	public string? Notes { get; set; }

	[JsonPropertyName("window_count")]
	public int WindowCount { get; set; }

	[JsonPropertyName("window_area")]
	public decimal? WindowArea { get; set; }

	[JsonPropertyName("plate_count")]
	public int PlateCount { get; set; }

	[JsonPropertyName("plate_area")]
	public decimal? PlateArea { get; set; }

	[JsonPropertyName("is_econom")]
	public bool IsEconom { get; set; }

	[JsonPropertyName("is_claim")]
	public bool IsClaim { get; set; }

	[JsonPropertyName("is_only_paid")]
	public bool IsOnlyPaid { get; set; }

	[JsonPropertyName("is_two_side_paint")]
	public bool IsTwoSidePaint { get; set; }

}