using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class OrderDto : OrderDtoBase
{
	[JsonPropertyName("window_count")]
	public int WindowCount { get; set; }

	[JsonPropertyName("window_area")]
	public decimal? WindowArea { get; set; }

	[JsonPropertyName("plate_count")]
	public int PlateCount { get; set; }

	[JsonPropertyName("plate_area")]
	public decimal? PlateArea { get; set; }
}