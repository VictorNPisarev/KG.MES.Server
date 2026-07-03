using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class SalesOrderListItemDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("order_number")]
	public string OrderNumber { get; set; } = string.Empty;

	[JsonPropertyName("ready_date")]
	public DateTime? ReadyDate { get; set; }

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

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("production_order_id")]
	public Guid? ProductionOrderId { get; set; }

	[JsonPropertyName("current_workplace_id")]
	public Guid? CurrentWorkplaceId { get; set; }

	[JsonPropertyName("current_status")]
	public string? CurrentStatus { get; set; }

	[JsonPropertyName("manager_id")]
	public Guid? ManagerId { get; set; }

	[JsonPropertyName("manager_name")]
	public string? ManagerName { get; set; }

	[JsonPropertyName("customer_id")]
	public Guid? CustomerId { get; set; }

	[JsonPropertyName("customer_name")]
	public string? CustomerName { get; set; }

	[JsonPropertyName("amount")]
	public decimal? Amount { get; set; }

	[JsonPropertyName("currency")]
	public string? Currency { get; set; }
}