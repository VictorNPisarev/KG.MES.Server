using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class RegisterRequestDto
{
	[JsonPropertyName("registrationToken")]
	public string RegistrationToken { get; set; } = string.Empty;

	[JsonPropertyName("fullName")]
	public string FullName { get; set; } = string.Empty;

	[JsonPropertyName("password")]
	public string Password { get; set; } = string.Empty;
}