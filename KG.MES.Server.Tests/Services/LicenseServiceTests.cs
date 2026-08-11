using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Services;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KG.MES.Server.Tests.Services;

[Trait("Category", "Unit")]
public class LicenseServiceTests : IDisposable
{
	private readonly AppDbContext _context;
	private readonly LicenseService _service;
	private readonly Mock<ILogger<LicenseService>> _loggerMock;

	public LicenseServiceTests()
	{
		// Создаем InMemory БД для каждого теста
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}")
			.Options;

		_context = new AppDbContext(options);
		_loggerMock = new Mock<ILogger<LicenseService>>();
		_service = new LicenseService(_context, _loggerMock.Object);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task CreateAsync_ShouldCreateLicenseWithValidKey()
	{
		// Act
		var license = await _service.CreateAsync("Test license", 30);

		// Assert
		license.Should().NotBeNull();
		license.Id.Should().NotBeEmpty();
		license.KeyCode.Should().NotBeNullOrEmpty();
		license.KeyCode.Should().MatchRegex(@"^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$");
		license.IsActive.Should().BeTrue();
		license.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
		license.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
		license.Notes.Should().Be("Test license");
	}

	[Fact]
	public async Task CreateAsync_WithoutExpiration_ShouldCreateLicenseWithoutExpiration()
	{
		// Act
		var license = await _service.CreateAsync("Test license", null);

		// Assert
		license.ExpiresAt.Should().BeNull();
	}

	[Fact]
	public async Task GetByKeyAsync_ShouldReturnLicense()
	{
		// Arrange
		var createdLicense = await _service.CreateAsync("Test license", 30);

		// Act
		var foundLicense = await _service.GetByKeyAsync(createdLicense.KeyCode);

		// Assert
		foundLicense.Should().NotBeNull();
		foundLicense!.Id.Should().Be(createdLicense.Id);
		foundLicense.KeyCode.Should().Be(createdLicense.KeyCode);
		foundLicense.IsActive.Should().BeTrue();
	}

	[Fact]
	public async Task GetByKeyAsync_WithInvalidKey_ShouldReturnNull()
	{
		// Act
		var foundLicense = await _service.GetByKeyAsync("INVALID-KEY");

		// Assert
		foundLicense.Should().BeNull();
	}

	[Fact]
	public async Task IsActiveAsync_ShouldReturnTrueForActiveLicense()
	{
		// Arrange
		var license = await _service.CreateAsync("Test license", 30);

		// Act
		var isActive = await _service.IsActiveAsync(license.Id);

		// Assert
		isActive.Should().BeTrue();
	}

	[Fact]
	public async Task IsActiveAsync_ShouldReturnFalseForExpiredLicense()
	{
		// Arrange
		var license = await _service.CreateAsync("Test license", -1); // Истекла вчера

		// Act
		var isActive = await _service.IsActiveAsync(license.Id);

		// Assert
		isActive.Should().BeFalse();
	}

	[Fact]
	public async Task IsActiveAsync_ShouldReturnFalseForRevokedLicense()
	{
		// Arrange
		var license = await _service.CreateAsync("Test license", 30);
		await _service.RevokeAsync(license.Id, "Test revocation");

		// Act
		var isActive = await _service.IsActiveAsync(license.Id);

		// Assert
		isActive.Should().BeFalse();
	}

	[Fact]
	public async Task RevokeAsync_ShouldDeactivateLicense()
	{
		// Arrange
		var license = await _service.CreateAsync("Test license", 30);

		// Act
		var result = await _service.RevokeAsync(license.Id, "Test revocation");

		// Assert
		result.Should().BeTrue();

		var revokedLicense = await _service.GetByIdAsync(license.Id);
		revokedLicense.Should().NotBeNull();
		revokedLicense!.IsActive.Should().BeFalse();
		revokedLicense.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
		revokedLicense.Notes.Should().Be("Test revocation");
	}

	[Fact]
	public async Task RevokeAsync_WithNonExistentLicense_ShouldReturnFalse()
	{
		// Act
		var result = await _service.RevokeAsync(Guid.NewGuid(), "Test");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task ValidateAndBindAsync_ShouldBindLicenseToNewDevice()
	{
		// Arrange
		var license = await _service.CreateAsync("Test license", 30);
		var deviceId = "TEST-DEVICE-001";
		var deviceName = "Test PC";

		// Act
		var result = await _service.ValidateAndBindAsync(
			license.KeyCode,
			deviceId,
			deviceName,
			"192.168.1.1"
		);

		// Assert
		result.Should().NotBeNull();
		result.IsValid.Should().BeTrue();
		result.LicenseId.Should().Be(license.Id);
		result.DeviceId.Should().NotBeEmpty();

		// Проверяем, что устройство привязалось
		var updatedLicense = await _service.GetByKeyAsync(license.KeyCode);
		updatedLicense.Should().NotBeNull();
		updatedLicense!.Device.Should().NotBeNull();
		updatedLicense.Device!.DeviceHardwareId.Should().Be(deviceId);
		updatedLicense.Device.DeviceName.Should().Be(deviceName);
		updatedLicense.Device.LastIp.Should().Be("192.168.1.1");
	}

	[Fact]
	public async Task ValidateAndBindAsync_WithSameDevice_ShouldUpdateLastUsed()
	{
		// Arrange
		var license = await _service.CreateAsync("Test license", 30);
		var deviceId = "TEST-DEVICE-001";

		// Первая привязка
		var firstResult = await _service.ValidateAndBindAsync(license.KeyCode, deviceId, "Test PC", "192.168.1.1");
		var firstDevice = await _context.Devices.FindAsync(firstResult.DeviceId);
		var firstUsedAt = firstDevice!.LastUsedAt;

		// Act - вторая привязка с тем же устройством
		await Task.Delay(100); // Чтобы время изменилось
		var secondResult = await _service.ValidateAndBindAsync(
			license.KeyCode,
			deviceId,
			"Test PC Updated",
			"192.168.1.2"
		);

		// Assert
		secondResult.IsValid.Should().BeTrue();

		var updatedLicense = await _service.GetByKeyAsync(license.KeyCode);
		updatedLicense.Should().NotBeNull();
		updatedLicense!.Device!.DeviceName.Should().Be("Test PC Updated");
		updatedLicense.Device.LastIp.Should().Be("192.168.1.2");
		updatedLicense.Device.LastUsedAt.Should().NotBeNull();
		updatedLicense.Device.LastUsedAt!.Value.Should().BeAfter(firstUsedAt!.Value);
	}

	[Fact]
	public async Task ValidateAndBindAsync_WithDifferentDevice_ShouldFail()
	{
		// Arrange
		var license = await _service.CreateAsync("Test license", 30);
		var deviceId1 = "TEST-DEVICE-001";

		// Привязываем к первому устройству
		await _service.ValidateAndBindAsync(license.KeyCode, deviceId1, "Test PC 1", null);

		// Act - пытаемся привязать к другому устройству
		var result = await _service.ValidateAndBindAsync(
			license.KeyCode,
			"TEST-DEVICE-002",
			"Test PC 2",
			null
		);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Reason.Should().Contain("already used on device");
	}

	[Fact]
	public async Task ValidateAndBindAsync_WithInvalidKey_ShouldFail()
	{
		// Act
		var result = await _service.ValidateAndBindAsync(
			"INVALID-KEY",
			"TEST-DEVICE-001",
			"Test PC",
			null
		);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Reason.Should().Be("Invalid license key");
	}

	[Fact]
	public async Task ValidateAndBindAsync_WithEmptyDeviceId_ShouldFail()
	{
		// Arrange
		var license = await _service.CreateAsync("Test license", 30);

		// Act
		var result = await _service.ValidateAndBindAsync(
			license.KeyCode,
			"",
			"Test PC",
			null
		);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Reason.Should().Be("Device hardware ID is required");
	}

	[Fact]
	public async Task ValidateAndBindAsync_WithExpiredLicense_ShouldFail()
	{
		// Arrange
		var license = await _service.CreateAsync("Test license", -1); // Истекла

		// Act
		var result = await _service.ValidateAndBindAsync(
			license.KeyCode,
			"TEST-DEVICE-001",
			"Test PC",
			null
		);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Reason.Should().Be("License has expired");
	}
}