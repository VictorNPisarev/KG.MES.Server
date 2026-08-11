using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace KG.MES.Server.Tests.Services;

[Trait("Category", "Unit")]
public class AuthServiceTests : IDisposable
{
	private readonly AppDbContext _context;
	private readonly AuthService _service;
	private readonly Mock<IUserService> _userServiceMock;
	private readonly Mock<ILicenseService> _licenseServiceMock;
	private readonly Mock<IJwtService> _jwtServiceMock;
	private readonly Mock<IUserDeviceService> _userDeviceServiceMock;
	private readonly Mock<ILogger<AuthController>> _loggerMock;

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

		_service = new AuthService(
			_context,
			_userServiceMock.Object,
			_licenseServiceMock.Object,
			_jwtServiceMock.Object,
			_userDeviceServiceMock.Object,
			_loggerMock.Object
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
}