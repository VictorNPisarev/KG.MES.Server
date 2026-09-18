using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

/// <summary>
/// Обертка в JSON
/// </summary>
public class OrderTraceResponse
{
	[JsonPropertyName("orders")]
	public List<OrderTraceDto> OrderTraces { get; set; } = new();
}
