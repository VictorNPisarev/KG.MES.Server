using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class LoginResultDto
{
	[JsonPropertyName("success")]
	public bool Success { get; set; }

	[JsonPropertyName("error")]
	public string? Error { get; set; }

	[JsonPropertyName("response")]
	public LoginResponseDto? Response { get; set; }

	public static LoginResultDto CreateSuccess(LoginResponseDto response) =>
		new()
		{
			Success = true,
			Response = response
		};

	public static LoginResultDto CreateFailure(string error) =>
		new()
		{
			Success = false,
			Error = error
		};
}