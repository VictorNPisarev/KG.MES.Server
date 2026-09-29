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

	[JsonPropertyName("registrationRequired")]
	public bool RegistrationRequired { get; set; } = false;


	public static LoginResultDto CreateRegistrationRequired(string token) => 
		new() 
		{ 
			Success = false,
			RegistrationRequired = true,
			Response = new LoginResponseDto 
						{ 
							RegistrationRequired = true, 
							RegistrationToken = token
						}
		};


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