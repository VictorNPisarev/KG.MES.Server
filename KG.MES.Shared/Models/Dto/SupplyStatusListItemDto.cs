using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class SupplyStatusListItemDto
{
	[JsonPropertyName("order_id")]
	public Guid OrderId { get; set; }

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

	[JsonPropertyName("alum_water_shield")]
	public string? AlumWaterShield { get; set; }
}