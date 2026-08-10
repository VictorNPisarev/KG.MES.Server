using KG.MES.Server.Data;
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public class AuthService : IAuthService
{
	private readonly AppDbContext _context;
	private readonly IUserService _userService;
	private readonly ILicenseService _licenseService;
	private readonly IJwtService _jwtService;
	private readonly IUserDeviceService _userDeviceService;
	private readonly ILogger<AuthController> _logger;

	public AuthService(AppDbContext context,
		IUserService userService,
		ILicenseService licenseService,
		IJwtService jwtService,
		IUserDeviceService userDeviceService,
		ILogger<AuthController> logger)
	{
		_context = context;
		_userService = userService;
		_licenseService = licenseService;
		_jwtService = jwtService;
		_userDeviceService = userDeviceService;
		_logger = logger;
	}

	public async Task<LoginResultDto> AuthenticateUserAsync(LoginRequestDto request, string? ipAddress = null)
	{
		// ============================================================
		// 1. ПРОВЕРЯЕМ ПОЛЬЗОВАТЕЛЯ
		// ============================================================
		var user = await _userService.AuthenticateAsync(request.Email, request.Password);
		if (user == null)
		{
			_logger.LogWarning("Login failed for {Email}", request.Email);
			return LoginResultDto.CreateFailure("Invalid email or password");
		}

		// ============================================================
		// 2. ПРОВЕРЯЕМ ЛИЦЕНЗИЮ И УСТРОЙСТВО
		// ============================================================
		var licenseResult = await _licenseService.ValidateAndBindAsync(
			request.LicenseKey,
			request.DeviceHardwareId,
			request.DeviceName ?? Environment.MachineName,
			ipAddress
		);

		if (licenseResult == null || !licenseResult.IsValid)
		{
			_logger.LogWarning(
				"License validation failed for user {Email}: {Reason}",
				request.Email, licenseResult?.Reason);
			return LoginResultDto.CreateFailure(licenseResult?.Reason ?? "License validation failed");
		}

		// ============================================================
		// 2.5. РЕГИСТРИРУЕМ ПАРУ ПОЛЬЗОВАТЕЛЬ-УСТРОЙСТВО (для аудита)
		// ============================================================
		await _userDeviceService.LinkUserDeviceAsync(user.Id, licenseResult.DeviceId);

		// ============================================================
		// 3. ВЫДАЁМ JWT ТОКЕН
		// ============================================================
		var token = _jwtService.GenerateToken(
			user.Id,
			user.Email,
			user.Role?.Name ?? "user"
		);

		_logger.LogInformation("User {Email} logged in successfully", request.Email);

		// ============================================================
		// 3.5 выдаю Refresh-токен
		// ============================================================
		var refreshToken = _jwtService.GenerateRefreshToken();

		var refreshExpiresAt = DateTime.UtcNow.AddDays(7);

		// Сохраняем Refresh-токен
		var refreshTokenEntity = new RefreshToken
		{
			Id = Guid.NewGuid(),
			UserId = user.Id,
			DeviceId = licenseResult.DeviceId,
			LicenseId = licenseResult.LicenseId,
			Token = refreshToken,
			ExpiresAt = refreshExpiresAt,
			CreatedAt = DateTime.UtcNow,
			IsRevoked = false
		};
		_context.RefreshTokens.Add(refreshTokenEntity);
		await _context.SaveChangesAsync();

		// ============================================================
		// 4. ФОРМИРУЕМ ОТВЕТ
		// ============================================================
		var response = new LoginResponseDto
		{
			AccessToken = token,
			RefreshToken = refreshToken,
			TokenType = "Bearer",
			ExpiresIn = 300,
			User = new UserDto
			{
				Id = user.Id,
				Email = user.Email,
				Name = user.Name,
				RoleId = user.RoleId,
				RoleName = user.Role?.Name,
				RoleLevel = user.Role?.Level ?? 10
			}
		};

		return LoginResultDto.CreateSuccess(response);
	}

	public async Task<LoginResultDto> RefreshAuthenticationToken(RefreshRequestDto request)
	{
		if (string.IsNullOrEmpty(request.RefreshToken))
			return LoginResultDto.CreateFailure("Refresh token is required");

		// 1. Проверяю Refresh-токен
		var refreshToken = await _context.RefreshTokens
			.Include(rt => rt.User)
			.ThenInclude(u => u!.Role)
			.Include(rt => rt.Device)
			.FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked);

		if (refreshToken == null)
			return LoginResultDto.CreateFailure("Invalid refresh token");

		if (refreshToken.ExpiresAt < DateTime.UtcNow)
			return LoginResultDto.CreateFailure("Refresh token expired");

		// 2. Проверяю устройство
		if (refreshToken.Device?.DeviceHardwareId != request.DeviceHardwareId)
			return LoginResultDto.CreateFailure("Device mismatch");

		// 3. Проверяю лицензию
		var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == refreshToken.Device.LicenseId && l.KeyCode == request.LicenseKey);

		if(license == null)
			return LoginResultDto.CreateFailure("Invalid license key");

		if (!license.IsActive)
			return LoginResultDto.CreateFailure("License is revoked");

		if (license.ExpiresAt.HasValue && license.ExpiresAt < DateTime.UtcNow)
			return LoginResultDto.CreateFailure("License expired");

		// 4. Генерируем новый Access Token
		var user = refreshToken.User ?? new();
		var newAccessToken = _jwtService.GenerateToken(
			user.Id,
			user.Email,
			user.Role?.Name ?? "user"
		);

		// 4. Обновляем Refresh-токен (опционально: продлеваем или выдаём новый)
		await _context.SaveChangesAsync();

		var response = new LoginResponseDto
		{
			AccessToken = newAccessToken,
			RefreshToken = refreshToken.Token,
			TokenType = "Bearer",
			ExpiresIn = 300,
			User = new UserDto
			{
				Id = user.Id,
				Email = user.Email,
				Name = user.Name,
				RoleId = user.RoleId,
				RoleName = user.Role?.Name,
				RoleLevel = user.Role?.Level ?? 10
			}
		};

		return LoginResultDto.CreateSuccess(response);
	}
}