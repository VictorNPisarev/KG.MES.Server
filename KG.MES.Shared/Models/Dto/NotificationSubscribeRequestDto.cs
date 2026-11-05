using System.Text.Json.Serialization;

public class NotificationSubscribeRequestDto
{
	[JsonPropertyName("event_id")]
	public Guid EventId { get; set; }

	[JsonPropertyName("role_id")]
	public Guid? RoleId { get; set; }

	[JsonPropertyName("user_id")]
	public Guid? UserId { get; set; }

	[JsonPropertyName("severity")]
	public string Severity { get; set; } = "Info";
}