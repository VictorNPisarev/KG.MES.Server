using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class OrderCommercialDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

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