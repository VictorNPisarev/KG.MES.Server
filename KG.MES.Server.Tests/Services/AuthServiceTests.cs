using FluentAssertions;
using KG.MES.Shared.Data;
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using KG.MES.Shared.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using KG.MES.Shared.Models.Enums;

namespace KG.MES.Server.Tests.Services;

[Trait("Category", "Unit")]
public class AuthServiceTests : IDisposable
{
	private readonly AppDbContext _context;
	private readonly ServerAuthService _service;
	private readonly Mock<IUserService> _userServiceMock;
	private readonly Mock<ILicenseService> _licenseServiceMock;
	private readonly Mock<IJwtService> _jwtServiceMock;
	private readonly Mock<IUserDeviceService> _userDeviceServiceMock;
	private readonly Mock<ILogger<AuthController>> _loggerMock;
	private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;

	public AuthServiceTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}")
			.ConfigureWarnings(warnings =>
				warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)) // ← Добавлено!
			.Options;

		_context = new AppDbContext(options);
		_userServiceMock = new Mock<IUserService>();
		_licenseServiceMock = new Mock<ILicenseService>();
		_jwtServiceMock = new Mock<IJwtService>();
		_userDeviceServiceMock = new Mock<IUserDeviceService>();
		_loggerMock = new Mock<ILogger<AuthController>>();
		_passwordHasherMock = new Mock<IPasswordHasher<User>>();

		_service = new ServerAuthService(
			_context,
			_userServiceMock.Object,
			_licenseServiceMock.Object,
			_jwtServiceMock.Object,
			_userDeviceServiceMock.Object,
			_loggerMock.Object,
			_passwordHasherMock.Object
		);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task RefreshAuthenticationToken_WithValidToken_ShouldReturnNewAccessToken()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var deviceId = Guid.NewGuid();
		var licenseId = Guid.NewGuid();
		var refreshToken = "valid-refresh-token";

		var user = new User
		{
			Id = userId,
			Email = "test@example.com",
			Name = "Test User",
			Role = new Role { Id = Guid.NewGuid(), Name = "Admin", Level = 50 }
		};

		var license = new License
		{
			Id = licenseId,
			KeyCode = "TEST-1234-5678-90AB",
			IsActive = true,
			ExpiresAt = DateTime.UtcNow.AddDays(30)
		};

		var device = new Device
		{
			Id = deviceId,
			DeviceHardwareId = "test-device-123",
			LicenseId = licenseId
		};

		var refreshTokenEntity = new RefreshToken
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			DeviceId = deviceId,
			Token = refreshToken,
			ExpiresAt = DateTime.UtcNow.AddDays(7),
			IsRevoked = false
		};

		// Сохраняем все в БД
		_context.Users.Add(user);
		_context.Licenses.Add(license);
		_context.Devices.Add(device);
		_context.RefreshTokens.Add(refreshTokenEntity);
		await _context.SaveChangesAsync();

		var request = new RefreshRequestDto
		{
			RefreshToken = refreshToken,
			DeviceHardwareId = "test-device-123",
			LicenseKey = "TEST-1234-5678-90AB"
		};

		_jwtServiceMock
			.Setup(s => s.GenerateToken(userId, user.Email, "Admin"))
			.Returns("new-access-token");

		// Act
		var result = await _service.RefreshAuthenticationToken(request);

		// Assert
		result.Success.Should().BeTrue();
		result.Response.Should().NotBeNull();
		result.Response!.AccessToken.Should().Be("new-access-token");
	}

	[Fact]
	public async Task RefreshAuthenticationToken_WithExpiredToken_ShouldReturnFailure()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var deviceId = Guid.NewGuid();
		var licenseId = Guid.NewGuid();
		var refreshToken = "expired-refresh-token";

		var license = new License
		{
			Id = licenseId,
			KeyCode = "TEST-1234-5678-90AB",
			IsActive = true
		};

		var device = new Device
		{
			Id = deviceId,
			DeviceHardwareId = "device-001",
			LicenseId = licenseId
		};

		var user = new User
		{
			Id = userId,
			Email = "test@example.com",
			Name = "Test User",
			Role = new Role { Id = Guid.NewGuid(), Name = "User", Level = 10 }
		};

		var refreshTokenEntity = new RefreshToken
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			DeviceId = deviceId,
			Token = refreshToken,
			ExpiresAt = DateTime.UtcNow.AddDays(-1),
			IsRevoked = false
		};

		_context.Licenses.Add(license);
		_context.Devices.Add(device);
		_context.Users.Add(user);
		_context.RefreshTokens.Add(refreshTokenEntity);

		await _context.SaveChangesAsync();

		var request = new RefreshRequestDto
		{
			RefreshToken = refreshToken,
			DeviceHardwareId = "test-device-123",
			LicenseKey = "TEST-1234-5678-90AB"
		};

		// Act
		var result = await _service.RefreshAuthenticationToken(request);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Refresh token expired"); // ← Исправлено!
	}

	[Fact]
	public async Task RefreshAuthenticationToken_WithDeviceMismatch_ShouldReturnFailure()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var deviceId = Guid.NewGuid();
		var licenseId = Guid.NewGuid();
		var refreshToken = "test-refresh-token";

		var license = new License
		{
			Id = licenseId,
			KeyCode = "TEST-1234-5678-90AB",
			IsActive = true
		};

		var device = new Device
		{
			Id = deviceId,
			DeviceHardwareId = "device-001",
			LicenseId = licenseId
		};

		var user = new User
		{
			Id = userId,
			Email = "test@example.com",
			Name = "Test User",
			Role = new Role { Id = Guid.NewGuid(), Name = "User", Level = 10 }
		};

		var refreshTokenEntity = new RefreshToken
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			DeviceId = deviceId,
			Token = refreshToken,
			ExpiresAt = DateTime.UtcNow.AddDays(7),
			IsRevoked = false
		};

		_context.Licenses.Add(license);
		_context.Devices.Add(device);
		_context.Users.Add(user);
		_context.RefreshTokens.Add(refreshTokenEntity);
		await _context.SaveChangesAsync();

		var request = new RefreshRequestDto
		{
			RefreshToken = refreshToken,
			DeviceHardwareId = "device-002",
			LicenseKey = "TEST-1234-5678-90AB"
		};

		// Act
		var result = await _service.RefreshAuthenticationToken(request);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Device mismatch");
	}

	[Fact]
	public async Task RefreshAuthenticationToken_WithInvalidLicense_ShouldReturnFailure()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var deviceId = Guid.NewGuid();
		var licenseId = Guid.NewGuid();
		var refreshToken = "test-refresh-token";

		var license = new License
		{
			Id = licenseId,
			KeyCode = "TEST-1234-5678-90AB",
			IsActive = true
		};

		var device = new Device
		{
			Id = deviceId,
			DeviceHardwareId = "test-device-123",
			LicenseId = licenseId
		};

		var user = new User
		{
			Id = userId,
			Email = "test@example.com",
			Name = "Test User",
			Role = new Role { Id = Guid.NewGuid(), Name = "User", Level = 10 }
		};

		var refreshTokenEntity = new RefreshToken
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			DeviceId = deviceId,
			Token = refreshToken,
			ExpiresAt = DateTime.UtcNow.AddDays(7),
			IsRevoked = false
		};

		_context.Licenses.Add(license);
		_context.Devices.Add(device);
		_context.Users.Add(user);
		_context.RefreshTokens.Add(refreshTokenEntity);
		await _context.SaveChangesAsync();

		var request = new RefreshRequestDto
		{
			RefreshToken = refreshToken,
			DeviceHardwareId = "test-device-123",
			LicenseKey = "DIFFERENT-KEY"
		};

		// Act
		var result = await _service.RefreshAuthenticationToken(request);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Invalid license key");
	}

	[Fact]
	public async Task AuthenticateUserAsync_WithInvalidPassword_ShouldReturnFailure()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var user = new User
		{
			Id = userId,
			Email = "test@example.com",
			PasswordHash = "hashed_password",
			IsActive = true,
			IsApproved = true
		};

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_passwordHasherMock
			.Setup(h => h.VerifyHashedPassword(user, "hashed_password", "WrongPassword!"))
			.Returns(PasswordVerificationResult.Failed);

		var request = new LoginRequestDto
		{
			Email = "test@example.com",
			Password = "WrongPassword!",
			LicenseKey = "TEST-KEY",
			DeviceHardwareId = "device-123"
		};

		// Act
		var result = await _service.AuthenticateUserAsync(request);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Invalid email or password");
		result.RegistrationRequired.Should().BeFalse();  // ← важно!
	}

	[Fact]
	public async Task AuthenticateUserAsync_WithNonExistentUserAndSingleDevice_ShouldReturnFailure()
	{
		// Arrange
		var request = new LoginRequestDto
		{
			Email = "new@example.com",
			Password = "Qwerty123!",
			LicenseKey = "SINGLE-DEVICE-KEY",
			DeviceHardwareId = "device-123"
		};

		_licenseServiceMock
			.Setup(l => l.CanSelfRegisterAsync("SINGLE-DEVICE-KEY"))
			.ReturnsAsync(false);

		// Act
		var result = await _service.AuthenticateUserAsync(request);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Invalid email or password");
		result.RegistrationRequired.Should().BeFalse();
	}

	[Fact]
	public async Task AuthenticateUserAsync_WithNonExistentUserAndMultiDevice_ShouldReturnRegistrationRequired()
	{
		// Arrange
		var licenseId = Guid.NewGuid();
		var license = new License
		{
			Id = licenseId,
			KeyCode = "MULTI-DEVICE-KEY",
			LicenseType = LicenseType.MultiDevice,
			IsActive = true
		};

		var request = new LoginRequestDto
		{
			Email = "new@example.com",
			Password = "Qwerty123!",
			LicenseKey = "MULTI-DEVICE-KEY",
			DeviceHardwareId = "device-123",
			DeviceName = "Test",
			ApplicationCode = "Masters"
		};

		_licenseServiceMock
			.Setup(l => l.CanSelfRegisterAsync("MULTI-DEVICE-KEY"))
			.ReturnsAsync(true);

		_licenseServiceMock
			.Setup(l => l.GetByKeyAsync("MULTI-DEVICE-KEY"))
			.ReturnsAsync(license);

		_jwtServiceMock
			.Setup(j => j.GenerateRegistrationToken(
				licenseId,
				"new@example.com",
				"Masters",
				"device-123",
				"Test"))
			.Returns("registration-token-xyz");

		// Act
		var result = await _service.AuthenticateUserAsync(request);

		// Assert
		result.Success.Should().BeFalse();
		result.RegistrationRequired.Should().BeTrue();
	}
}