using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class CommerceOrderDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("order_id")]
	public Guid OrderId { get; set; }

	[JsonPropertyName("manager")]
	public string? Manager { get; set; }

	[JsonPropertyName("customer_name")]
	public string? CustomerName { get; set; }

	[JsonPropertyName("price")]
	public decimal Price { get; set; }

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("updated_at")]
	public DateTime? UpdatedAt { get; set; }
}