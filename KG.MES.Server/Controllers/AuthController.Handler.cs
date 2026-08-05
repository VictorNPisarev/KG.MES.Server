
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

public partial class AuthController
{
	private readonly IUserService _userService;
	private readonly ILicenseService _licenseService;
	private readonly IJwtService _jwtService;
	private readonly ILogger<AuthController> _logger;

	public AuthController(
		IUserService userService,
		ILicenseService licenseService,
		IJwtService jwtService,
		ILogger<AuthController> logger)
	{
		_userService = userService;
		_licenseService = licenseService;
		_jwtService = jwtService;
		_logger = logger;
	}

	public async Task<IActionResult> LoginHandler(LoginRequestDto request)
	{
		// ============================================================
		// 1. ПРОВЕРЯЕМ ПОЛЬЗОВАТЕЛЯ
		// ============================================================
		var user = await _userService.AuthenticateAsync(request.Email, request.Password);
		if (user == null)
		{
			_logger.LogWarning("Login failed for {Email}", request.Email);
			return Unauthorized(new { error = "Invalid email or password" });
		}

		// ============================================================
		// 2. ПРОВЕРЯЕМ ЛИЦЕНЗИЮ И УСТРОЙСТВО
		// ============================================================
		var licenseResult = await _licenseService.ValidateAndBindAsync(
			request.LicenseKey,
			request.DeviceId,
			request.DeviceName ?? Environment.MachineName,
			HttpContext.Connection.RemoteIpAddress?.ToString()
		);

		if (!licenseResult.IsValid)
		{
			_logger.LogWarning(
				"License validation failed for user {Email}: {Reason}",
				request.Email, licenseResult.Reason);
			return Unauthorized(new { error = licenseResult.Reason });
		}

		// ============================================================
		// 3. ВЫДАЁМ JWT ТОКЕН
		// ============================================================
		var token = _jwtService.GenerateToken(
			user.Id,
			user.Email,
			user.Role?.Name ?? "user"
		);

		_logger.LogInformation("User {Email} logged in successfully", request.Email);

		return Ok(new
		{
			access_token = token,
			token_type = "Bearer",
			expires_in = 3600, // 1 час
			user = new
			{
				user.Id,
				user.Email,
				user.Name,
				role = user.Role?.Name
			}
		});
	}
}