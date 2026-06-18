using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class OrderBlockDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("reason")]
	public string? Reason { get; set; }

	[JsonPropertyName("blockedAt")]
	public DateTime BlockedAt { get; set; }

	[JsonPropertyName("userId")]
	public Guid? UserId { get; set; }
}