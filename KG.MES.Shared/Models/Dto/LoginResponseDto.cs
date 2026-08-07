using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class LoginResponseDto
{
	[JsonPropertyName("access_token")]
	public string AccessToken { get; set; } = string.Empty;

	[JsonPropertyName("refresh_token")]
	public string RefreshToken { get; set; } = string.Empty;

	[JsonPropertyName("token_type")]
	public string TokenType { get; set; } = "Bearer";

	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }

	[JsonPropertyName("user")]
	public UserDto? User { get; set; }
}