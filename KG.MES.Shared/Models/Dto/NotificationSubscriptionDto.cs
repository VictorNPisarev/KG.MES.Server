using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class NotificationSubscriptionDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("event_id")]
	public Guid EventId { get; set; }

	[JsonPropertyName("event_code")]
	public string EventCode { get; set; } = string.Empty;

	[JsonPropertyName("event_description")]
	public string EventDescription { get; set; } = string.Empty;

	[JsonPropertyName("role_id")]
	public Guid? RoleId { get; set; }

	[JsonPropertyName("role_name")]
	public string? RoleName { get; set; }

	[JsonPropertyName("user_id")]
	public Guid? UserId { get; set; }

	[JsonPropertyName("user_name")]
	public string? UserName { get; set; }

	[JsonPropertyName("severity")]
	public string Severity { get; set; } = "Info";

	[JsonPropertyName("is_active")]
	public bool IsActive { get; set; }
}