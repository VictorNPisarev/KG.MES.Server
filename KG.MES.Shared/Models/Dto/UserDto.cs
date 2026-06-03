using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class UserDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("role_id")]
	public Guid? RoleId { get; set; }

	[JsonPropertyName("role_name")]
	public string? RoleName { get; set; }

	[JsonPropertyName("role_level")]
	public int RoleLevel { get; set; }
}
