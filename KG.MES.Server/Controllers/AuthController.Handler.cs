
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.AspNetCore.Mvc;

public partial class AuthController
{
	private readonly IAuthService _authService;
	private readonly ILogger<AuthController> _logger;

	public AuthController(
		ILogger<AuthController> logger, 
		IAuthService authService)
	{
		_authService = authService;
		_logger = logger;
	}

	public async Task<IActionResult> LoginHandler(LoginRequestDto request)
	{
		var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
		var result = await _authService.AuthenticateUserAsync(request, ipAddress);

		if (!result.Success)
		{
			return Unauthorized(new { error = result.Error });
		}

		return Ok(result.Response);
	}

	public async Task<IActionResult> RefreshHandler(RefreshRequestDto request)
	{
		var result = await _authService.RefreshAuthenticationToken(request);

		if (!result.Success)
		{
			return Unauthorized(new { error = result.Error });
		}

		return Ok(result.Response);
	}
}