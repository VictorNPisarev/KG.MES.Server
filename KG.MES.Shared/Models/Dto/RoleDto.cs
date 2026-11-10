using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class RoleDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("level")]
	public int Level { get; set; }
}