using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class NotificationEventDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("event_code")]
	public string EventCode { get; set; } = string.Empty;

	[JsonPropertyName("description")]
	public string Description { get; set; } = string.Empty;
}