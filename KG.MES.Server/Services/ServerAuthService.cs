using KG.MES.Shared.Data;
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using KG.MES.Shared.Services.Interfaces;
using KG.MES.Shared.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace KG.MES.Server.Services;

public class ServerAuthService : IAuthService
{
	private readonly AppDbContext context;
	private readonly IUserService userService;
	private readonly ILicenseService licenseService;
	private readonly IJwtService jwtService;
	private readonly IUserDeviceService userDeviceService;
	private readonly ILogger<AuthController> logger;
	private readonly IPasswordHasher<User> passwordHasher;


	public ServerAuthService(AppDbContext context,
		IUserService userService,
		ILicenseService licenseService,
		IJwtService jwtService,
		IUserDeviceService userDeviceService,
		ILogger<AuthController> logger,
		IPasswordHasher<User> passwordHasher)
	{
		this.context = context;
		this.userService = userService;
		this.licenseService = licenseService;
		this.jwtService = jwtService;
		this.userDeviceService = userDeviceService;
		this.logger = logger;
		this.passwordHasher = passwordHasher;
	}

	public async Task<LoginResultDto> AuthenticateUserAsync(LoginRequestDto request, string? ipAddress = null)
	{
		// ============================================================
		// 1. ПРОВЕРЯЕМ ПОЛЬЗОВАТЕЛЯ
		// ============================================================
		var user = await userService.AuthenticateAsync(request.Email, request.Password);

		// 1.1 Пользователь не найден → возможна self-registration
		if (user == null)
		{
			var canRegister = await licenseService.CanSelfRegisterAsync(request.LicenseKey);

			if (!canRegister)
			{
				logger.LogWarning("Login failed for {Email}", request.Email);
				return LoginResultDto.CreateFailure("Invalid email or password");
			}

			// Генерируею registration-токен
			var license = await licenseService.GetByKeyAsync(request.LicenseKey);
			if (license == null)
			{
				logger.LogWarning("License not found: {LicenseKey}", request.LicenseKey);
				return LoginResultDto.CreateFailure("Invalid license");
			}

			var registrationToken = jwtService.GenerateRegistrationToken(
				license.Id,
				request.Email,
				request.ApplicationCode,
				request.DeviceHardwareId,
				request.DeviceName);

			logger.LogInformation(
				"Registration required for {Email} (license: {LicenseKey})",
				request.Email, request.LicenseKey);

			return LoginResultDto.CreateRegistrationRequired(registrationToken);
		}

		// 1.2. Пользователь найден, но не подтверждён → проверяю дедлайн
		if (!user.IsApproved)
		{
			if (user.ApprovalDeadline == null || user.ApprovalDeadline < DateTime.UtcNow)
			{
				logger.LogWarning(
					"Login blocked for {Email}: approval expired",
					request.Email);

				return LoginResultDto.CreateFailure(
					"Учётная запись ожидает подтверждения администратора");
			}

			logger.LogWarning(
				"User {Email} logged in without approval (deadline: {Deadline})",
				user.Email, user.ApprovalDeadline);
		}
		// ============================================================
		// 2. ПРОВЕРЯЕМ ЛИЦЕНЗИЮ И УСТРОЙСТВО
		// ============================================================
		var licenseResult = await licenseService.ValidateAndBindAsync(
			request.LicenseKey,
			request.DeviceHardwareId,
			request.DeviceName ?? Environment.MachineName,
			ipAddress
		);

		if (licenseResult == null || !licenseResult.IsValid)
		{
			logger.LogWarning(
				"License validation failed for user {Email}: {Reason}",
				request.Email, licenseResult?.Reason);
			return LoginResultDto.CreateFailure(licenseResult?.Reason ?? "License validation failed");
		}

		// ============================================================
		// 2.5. РЕГИСТРИРУЕМ ПАРУ ПОЛЬЗОВАТЕЛЬ-УСТРОЙСТВО (для аудита)
		// ============================================================
		await userDeviceService.LinkUserDeviceAsync(user.Id, licenseResult.DeviceId);

		// ============================================================
		// 3. ВЫДАЁМ JWT ТОКЕН
		// ============================================================
		var token = jwtService.GenerateToken(
			user.Id,
			user.Email,
			user.Role?.Name ?? "user"
		);

		logger.LogInformation("User {Email} logged in successfully", request.Email);

		// ============================================================
		// 3.5 выдаю Refresh-токен
		// ============================================================
		var refreshToken = jwtService.GenerateRefreshToken();

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
		context.RefreshTokens.Add(refreshTokenEntity);
		await context.SaveChangesAsync();

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
				RoleLevel = user.Role?.Level ?? 10,
				IsPasswordSet = user.IsPasswordSet
			}
		};

		return LoginResultDto.CreateSuccess(response);
	}

	public async Task<LoginResultDto> RefreshAuthenticationToken(RefreshRequestDto request)
	{
		if (string.IsNullOrEmpty(request.RefreshToken))
			return LoginResultDto.CreateFailure("Refresh token is required");

		// 1. Проверяю Refresh-токен
		var refreshToken = await context.RefreshTokens
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
		var license = await context.Licenses.FirstOrDefaultAsync(l => l.Id == refreshToken.Device.LicenseId && l.KeyCode == request.LicenseKey);

		if(license == null)
			return LoginResultDto.CreateFailure("Invalid license key");

		if (!license.IsActive)
			return LoginResultDto.CreateFailure("License is revoked");

		if (license.ExpiresAt.HasValue && license.ExpiresAt < DateTime.UtcNow)
			return LoginResultDto.CreateFailure("License expired");

		// 4. Генерируем новый Access Token
		var user = refreshToken.User ?? new();
		var newAccessToken = jwtService.GenerateToken(
			user.Id,
			user.Email,
			user.Role?.Name ?? "user"
		);

		// 4. Обновляем Refresh-токен (опционально: продлеваем или выдаём новый)
		await context.SaveChangesAsync();

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
				RoleLevel = user.Role?.Level ?? 10,
				IsPasswordSet = user.IsPasswordSet
			}
		};

		return LoginResultDto.CreateSuccess(response);
	}

	public async Task<LoginResultDto> RegisterUserAsync(
		RegisterRequestDto request,
		string? ipAddress = null)
	{
		// 1. Валидируем registration-токен
		var payload = jwtService.ValidateRegistrationToken(request.RegistrationToken);
		if (payload == null)
		{
			logger.LogWarning("Invalid or expired registration token");
			return LoginResultDto.CreateFailure("Invalid or expired registration token");
		}

		// 2. Проверяем лицензию
		var license = await licenseService.GetByIdAsync(payload.LicenseId);
		if (license == null || !license.IsActive)
		{
			logger.LogWarning("License not found or inactive: {LicenseId}", payload.LicenseId);
			return LoginResultDto.CreateFailure("License is inactive");
		}

		if (license.LicenseType != LicenseType.MultiDevice)
		{
			logger.LogWarning("Self-registration not allowed for license type: {Type}", license.LicenseType);
			return LoginResultDto.CreateFailure("Self-registration is not allowed");
		}

		if (license.ExpiresAt.HasValue && license.ExpiresAt < DateTime.UtcNow)
		{
			logger.LogWarning("License expired: {LicenseId}", payload.LicenseId);
			return LoginResultDto.CreateFailure("License has expired");
		}

		// 3. Проверяем email
		var existingUser = await userService.GetUserByEmailAsync(payload.Email);
		if (existingUser != null)
		{
			logger.LogWarning("Email already registered: {Email}", payload.Email);
			return LoginResultDto.CreateFailure("Email already registered");
		}

		// 4. Определяем роль по applicationCode
		var roleName = GetRoleFromApplicationCode(payload.ApplicationCode);
		var role = await context.Roles
			.FirstOrDefaultAsync(r => r.Name == roleName);

		if (role == null)
		{
			logger.LogWarning("Role not found: {RoleName}", roleName);
			return LoginResultDto.CreateFailure($"Role '{roleName}' not found");
		}

		// 5. Создаём пользователя
		var now = DateTime.UtcNow;
		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = payload.Email,
			Name = request.FullName,
			RoleId = role.Id,
			IsActive = true,
			IsPasswordSet = true,
			IsApproved = false,
			ApprovalDeadline = now.AddDays(7),
			CreatedAt = now
		};

		user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

		context.Users.Add(user);
		await context.SaveChangesAsync();

		logger.LogInformation(
			"User {Email} registered (pending approval until {Deadline})",
			user.Email, user.ApprovalDeadline);

		// 6. Привязываем устройство к лицензии
		var licenseResult = await licenseService.ValidateAndBindAsync(
			license.KeyCode,
			payload.DeviceHardwareId,
			payload.DeviceName ?? Environment.MachineName,
			ipAddress);

		if (licenseResult == null || !licenseResult.IsValid)
		{
			logger.LogWarning(
				"Device binding failed during registration for {Email}: {Reason}",
				user.Email, licenseResult?.Reason);

			context.Users.Remove(user);
			await context.SaveChangesAsync();

			return LoginResultDto.CreateFailure(
				licenseResult?.Reason ?? "Device binding failed");
		}

		// 7. Связываем пользователя с устройством
		await userDeviceService.LinkUserDeviceAsync(user.Id, licenseResult.DeviceId);

		// 8. Генерируем access-токен
		var accessToken = jwtService.GenerateToken(
			user.Id,
			user.Email,
			role.Name);

		// 9. Генерируем refresh-токен
		var refreshToken = jwtService.GenerateRefreshToken();
		var refreshExpiresAt = DateTime.UtcNow.AddDays(7);

		var refreshTokenEntity = new RefreshToken
		{
			Id = Guid.NewGuid(),
			UserId = user.Id,
			DeviceId = licenseResult.DeviceId,
			LicenseId = license.Id,
			Token = refreshToken,
			ExpiresAt = refreshExpiresAt,
			CreatedAt = DateTime.UtcNow,
			IsRevoked = false
		};
		context.RefreshTokens.Add(refreshTokenEntity);
		await context.SaveChangesAsync();

		// 10. Формируем ответ
		var response = new LoginResponseDto
		{
			AccessToken = accessToken,
			RefreshToken = refreshToken,
			TokenType = "Bearer",
			ExpiresIn = 300,
			User = new UserDto
			{
				Id = user.Id,
				Email = user.Email,
				Name = user.Name,
				RoleId = user.RoleId,
				RoleName = role.Name,
				RoleLevel = role.Level
			}
		};

		logger.LogInformation("User {Email} registered and logged in", user.Email);

		return LoginResultDto.CreateSuccess(response);
	}

	private static string GetRoleFromApplicationCode(string? applicationCode)
	{
		return applicationCode switch
		{
			"Masters" => "Middle",
			"Sales" => "Simple",
			"Supply" => "LumberSupply",
			"Main" => "Advanced",
			_ => "Simple"
		};
	}
}