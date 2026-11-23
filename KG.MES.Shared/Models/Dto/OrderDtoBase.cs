using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public abstract class OrderDtoBase
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

	[JsonPropertyName("is_econom")]
	public bool IsEconom { get; set; }

	[JsonPropertyName("is_claim")]
	public bool IsClaim { get; set; }

	[JsonPropertyName("is_only_paid")]
	public bool IsOnlyPaid { get; set; }

	[JsonPropertyName("is_two_side_paint")]
	public bool IsTwoSidePaint { get; set; }

	[JsonPropertyName("production_order_id")]
	public Guid ProductionOrderId { get; set; }

	[JsonPropertyName("current_workplace_id")]
	public Guid? CurrentWorkplaceId { get; set; }

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("machine")]
	public string? Machine { get; set; }

	[JsonPropertyName("is_new")]
	public bool IsNew { get; set; }

	[JsonPropertyName("is_completed_on_current_workplace")]
	public bool IsCompletedOnCurrentWorkplace { get; set; }

	[JsonPropertyName("is_favorite")]
	public bool IsFavorite { get; set; }


}