using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class ActiveOrderDto
{
	[JsonPropertyName("order_number")]
	public string OrderNumber { get; set; } = string.Empty;

	[JsonPropertyName("started_at")]
	public DateTime StartedAt { get; set; }

	[JsonPropertyName("hours_in_work")]
	public double HoursInWork { get; set; }
}