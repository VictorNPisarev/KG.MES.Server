using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class SupplyStatusListItemDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("order_number")]
	public string OrderNumber { get; set; } = string.Empty;

	[JsonPropertyName("ready_date")]
	public DateTime? ReadyDate { get; set; }

	[JsonPropertyName("production_order_id")]
	public Guid ProductionOrderId { get; set; }

	[JsonPropertyName("current_workplace_id")]
	public Guid? CurrentWorkplaceId { get; set; }

	[JsonPropertyName("current_status")]
	public string? CurrentStatus { get; set; }

	[JsonPropertyName("lumber")]
	public string? Lumber { get; set; }

	[JsonPropertyName("paint")]
	public string? Paint { get; set; }

	[JsonPropertyName("glass")]
	public string? Glass { get; set; }

	[JsonPropertyName("furniture")]
	public string? Furniture { get; set; }

	[JsonPropertyName("alumWaterShield")]
	public string? AlumWaterShield { get; set; }

	[JsonPropertyName("lumber_comment")]
	public string? LumberComment { get; set; }

	[JsonPropertyName("paint_comment")]
	public string? PaintComment { get; set; }

	[JsonPropertyName("glass_comment")]
	public string? GlassComment { get; set; }

	[JsonPropertyName("furniture_comment")]
	public string? FurnitureComment { get; set; }

	[JsonPropertyName("alumWaterShield_comment")]
	public string? AlumWaterShieldComment { get; set; }

	[JsonPropertyName("windowsill")]
	public string? Windowsill { get; set; }

	[JsonPropertyName("windowsill_comment")]
	public string? WindowsillComment { get; set; }

	[JsonPropertyName("machine")]
	public string? Machine { get; set; }

	[JsonPropertyName("rtm_date")]
	public DateTime? RtmDate { get; set; }
}