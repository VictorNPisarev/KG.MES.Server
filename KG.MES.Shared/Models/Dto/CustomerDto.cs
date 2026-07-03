using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class CustomerDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("inn")]
	public string? Inn { get; set; }

	[JsonPropertyName("phone")]
	public string? Phone { get; set; }

	[JsonPropertyName("email")]
	public string? Email { get; set; }

	[JsonPropertyName("address")]
	public string? Address { get; set; }
}