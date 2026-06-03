using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class UserWorkplaceDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("previous_workplace_id")]
	public Guid? PreviousWorkplaceId { get; set; }

	[JsonPropertyName("is_workplace")]
	public bool IsWorkplace { get; set; }
}