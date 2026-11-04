using System.Text.Json.Serialization;

namespace KG.MES.Server.Models.Dto;

public class SetPasswordRequestDto
{
	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	[JsonPropertyName("newPassword")]
	public string NewPassword { get; set; } = string.Empty;
}