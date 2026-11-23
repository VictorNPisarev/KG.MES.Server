using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class SalesOrderDto : OrderDtoBase
{
	[JsonPropertyName("window_count")]
	public int WindowCount { get; set; }

	[JsonPropertyName("window_area")]
	public decimal? WindowArea { get; set; }

	[JsonPropertyName("plate_count")]
	public int PlateCount { get; set; }

	[JsonPropertyName("plate_area")]
	public decimal? PlateArea { get; set; }

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