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
}