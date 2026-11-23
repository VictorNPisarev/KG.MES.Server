using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class MastersOrderDto : OrderDtoBase
{
	[JsonPropertyName("window_count")]
	public int WindowCount { get; set; }

	[JsonPropertyName("window_area")]
	public double? WindowArea { get; set; }

	[JsonPropertyName("plate_count")]
	public int PlateCount { get; set; }

	[JsonPropertyName("plate_area")]
	public double? PlateArea { get; set; }

	[JsonPropertyName("customer_name")]
	public string CustomerName { get; set; } = string.Empty;

	[JsonPropertyName("current_workplace_name")]
	public string? CurrentWorkplaceName { get; set; }
}