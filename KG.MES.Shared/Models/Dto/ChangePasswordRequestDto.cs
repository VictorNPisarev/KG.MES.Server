using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class ChangePasswordRequestDto
{
	[JsonPropertyName("current_password")]
	public string CurrentPassword { get; set; } = string.Empty;

	[JsonPropertyName("new_password")]
	public string NewPassword { get; set; } = string.Empty;
}