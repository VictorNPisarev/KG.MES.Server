
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc;

public partial class AuthController
{
	private readonly IAuthService authService;
	private readonly ILogger<AuthController> logger;

	public AuthController(
		ILogger<AuthController> logger, 
		IAuthService authService)
	{
		this.authService = authService;
		this.logger = logger;
	}

	public async Task<IActionResult> LoginHandler(LoginRequestDto request)
	{
		var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
		var result = await authService.AuthenticateUserAsync(request, ipAddress);

		if (!result.Success)
		{
			// ✅ Если требуется регистрация — отдаём 200 с флагом, 
			// чтобы клиент мог показать форму
			if (result.RegistrationRequired)
				return Ok(new
				{
					registrationRequired = true,
					registrationToken = result.RegistrationToken
				});

			return Unauthorized(new { error = result.Error });
		}

		return Ok(result.Response);
	}

	public async Task<IActionResult> RefreshHandler(RefreshRequestDto request)
	{
		var result = await authService.RefreshAuthenticationToken(request);

		if (!result.Success)
		{
			return Unauthorized(new { error = result.Error });
		}

		return Ok(result.Response);
	}

	public async Task<IActionResult> RegisterHandler(RegisterRequestDto request)
	{
		var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
		var result = await authService.RegisterUserAsync(request, ipAddress);

		if (!result.Success)
		{
			return BadRequest(new { error = result.Error });
		}

		return Ok(result.Response);
	}
}