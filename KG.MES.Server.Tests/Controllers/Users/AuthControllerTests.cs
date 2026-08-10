using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Tests.Helpers;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KG.MES.Server.Tests.Controllers.Users;

[Trait("Category", "Users")]
public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public AuthControllerTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task Login_WithValidCredentials_ShouldReturnTokens()
	{
		// Arrange
		var customFactory = SetupTestFactory();
		var client = customFactory.CreateClient();


		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;

		new TestDataBuilder()
			.WithRole(r => { r.Name = "Middle"; r.Level = 10; createdRole = r; })
			.WithUser(u =>
			{
				u.Email = "test@example.com";
				u.Name = "Тестовый пользователь";
				u.RoleId = createdRole?.Id;
				u.IsPasswordSet = true;
				createdUser = u;
			})
			.WithLicense(l =>
			{
				l.KeyCode = "TEST-1234-5678-90AB";
				l.IsActive = true;
				l.ExpiresAt = DateTime.UtcNow.AddDays(30);
				createdLicense = l;
			})
			.Build(customFactory.Services);

		// Устанавливаем пароль через PasswordHasher (не через TestDataBuilder, т.к. он не знает про хеширование)
		using (var scope = customFactory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var user = await db.Users.FirstAsync(u => u.Email == "test@example.com");
			var passwordHasher = new PasswordHasher<User>();
			user.PasswordHash = passwordHasher.HashPassword(user, "Qwerty123!");
			await db.SaveChangesAsync();
		}

		var loginRequest = new
		{
			email = "test@example.com",
			password = "Qwerty123!",
			licenseKey = "TEST-1234-5678-90AB",
			deviceHardwareId = "test-device-123",
			deviceName = "Test PC"
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

		var json = await ParseJsonResponse(response);

		json.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
		json.GetProperty("refresh_token").GetString().Should().NotBeNullOrEmpty();
		json.GetProperty("token_type").GetString().Should().Be("Bearer");
		json.GetProperty("expires_in").GetInt32().Should().Be(300);

		var responseUser = json.GetProperty("user");
		responseUser.GetProperty("email").GetString().Should().Be("test@example.com");
		responseUser.GetProperty("name").GetString().Should().Be("Тестовый пользователь");
		responseUser.GetProperty("role_name").GetString().Should().Be("Middle");
	}

	[Fact]
	public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
	{
		// Arrange
		var customFactory = SetupTestFactory();
		var client = customFactory.CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;

		new TestDataBuilder()
			.WithRole(r => { r.Name = "Middle"; r.Level = 10; createdRole = r; })
			.WithUser(u =>
			{
				u.Email = "test@example.com";
				u.Name = "Тестовый пользователь";
				u.RoleId = createdRole?.Id;
				u.IsPasswordSet = true;
				createdUser = u;
			})
			.WithLicense(l =>
			{
				l.KeyCode = "TEST-1234-5678-90AB";
				l.IsActive = true;
				createdLicense = l;
			})
			.Build(customFactory.Services);

		using (var scope = customFactory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var user = await db.Users.FirstAsync(u => u.Email == "test@example.com");
			var passwordHasher = new PasswordHasher<User>();
			user.PasswordHash = passwordHasher.HashPassword(user, "Qwerty123!");
			await db.SaveChangesAsync();
		}

		var loginRequest = new
		{
			email = "test@example.com",
			password = "WrongPassword!",
			licenseKey = "TEST-1234-5678-90AB",
			deviceHardwareId = "test-device-123"
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("Invalid email or password");
	}

	[Fact]
	public async Task Login_WithExpiredLicense_ShouldReturnUnauthorized()
	{
		// Arrange
		var customFactory = SetupTestFactory();
		var client = customFactory.CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;

		new TestDataBuilder()
			.WithRole(r => { r.Name = "Middle"; r.Level = 10; createdRole = r; })
			.WithUser(u =>
			{
				u.Email = "test@example.com";
				u.Name = "Тестовый пользователь";
				u.RoleId = createdRole?.Id;
				u.IsPasswordSet = true;
				createdUser = u;
			})
			.WithLicense(l =>
			{
				l.KeyCode = "EXPIRED-1234-5678-90AB";
				l.IsActive = true;
				l.ExpiresAt = DateTime.UtcNow.AddDays(-1);
				createdLicense = l;
			})
			.Build(customFactory.Services);

		using (var scope = customFactory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var user = await db.Users.FirstAsync(u => u.Email == "test@example.com");
			var passwordHasher = new PasswordHasher<User>();
			user.PasswordHash = passwordHasher.HashPassword(user, "Qwerty123!");
			await db.SaveChangesAsync();
		}

		var loginRequest = new
		{
			email = "test@example.com",
			password = "Qwerty123!",
			licenseKey = "EXPIRED-1234-5678-90AB",
			deviceHardwareId = "test-device-123"
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("License has expired");
	}

	[Fact]
	public async Task Refresh_WithValidToken_ShouldReturnNewAccessToken()
	{
		// Arrange
		var customFactory = SetupTestFactory();
		var client = customFactory.CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;
		Device? createdDevice = null;
		RefreshToken? createdRefreshToken = null;

		new TestDataBuilder()
			.WithRole(r => { r.Name = "Middle"; r.Level = 10; createdRole = r; })
			.WithUser(u =>
			{
				u.Email = "test@example.com";
				u.Name = "Тестовый пользователь";
				u.RoleId = createdRole?.Id;
				u.IsPasswordSet = true;
				createdUser = u;
			})
			.WithLicense(l =>
			{
				l.KeyCode = "TEST-1234-5678-90AB";
				l.IsActive = true;
				l.ExpiresAt = DateTime.UtcNow.AddDays(30);
				createdLicense = l;
			})
			.WithDevice(d =>
			{
				d.DeviceHardwareId = "test-device-123";
				d.DeviceName = "Test PC";
				d.LicenseId = createdLicense!.Id;
				createdDevice = d;
			})
			.WithRefreshToken(rt =>
			{
				rt.UserId = createdUser!.Id;
				rt.DeviceId = createdDevice!.Id;
				rt.Token = "test-refresh-token";
				rt.ExpiresAt = DateTime.UtcNow.AddDays(7);
				rt.IsRevoked = false;
				createdRefreshToken = rt;
			})
			.Build(customFactory.Services);

		// Act
		var response = await client.PostAsJsonAsync("/api/auth/refresh", new
		{
			refresh_token = "test-refresh-token",
			device_hardware_id = "test-device-123"
		});

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

		var json = await ParseJsonResponse(response);
		json.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
		json.GetProperty("token_type").GetString().Should().Be("Bearer");
		json.GetProperty("expires_in").GetInt32().Should().Be(300);
	}

	[Fact]
	public async Task Refresh_WithRevokedToken_ShouldReturnUnauthorized()
	{
		// Arrange
		var customFactory = SetupTestFactory();
		var client = customFactory.CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;
		Device? createdDevice = null;
		RefreshToken? createdRefreshToken = null;

		new TestDataBuilder()
			.WithRole(r => { r.Name = "Middle"; r.Level = 10; createdRole = r; })
			.WithUser(u =>
			{
				u.Email = "test@example.com";
				u.Name = "Тестовый пользователь";
				u.RoleId = createdRole?.Id;
				u.IsPasswordSet = true;
				createdUser = u;
			})
			.WithLicense(l =>
			{
				l.KeyCode = "TEST-1234-5678-90AB";
				l.IsActive = true;
				l.ExpiresAt = DateTime.UtcNow.AddDays(30);
				createdLicense = l;
			})
			.WithDevice(d =>
			{
				d.DeviceHardwareId = "test-device-123";
				d.DeviceName = "Test PC";
				d.LicenseId = createdLicense!.Id;
				createdDevice = d;
			})
			.WithRefreshToken(rt =>
			{
				rt.UserId = createdUser!.Id;
				rt.DeviceId = createdDevice!.Id;
				rt.Token = "test-refresh-token";
				rt.ExpiresAt = DateTime.UtcNow.AddDays(7);
				rt.IsRevoked = true;  // ← отозван
				createdRefreshToken = rt;
			})
			.Build(customFactory.Services);

		// Act
		var response = await client.PostAsJsonAsync("/api/auth/refresh", new
		{
			refresh_token = "test-refresh-token",
			device_hardware_id = "test-device-123"
		});

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("Invalid refresh token");
	}

	[Fact]
	public async Task Refresh_WithDeviceMismatch_ShouldReturnUnauthorized()
	{
		// Arrange
		var customFactory = SetupTestFactory();
		var client = customFactory.CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;
		Device? createdDevice = null;
		RefreshToken? createdRefreshToken = null;

		new TestDataBuilder()
			.WithRole(r => { r.Name = "Middle"; r.Level = 10; createdRole = r; })
			.WithUser(u =>
			{
				u.Email = "test@example.com";
				u.Name = "Тестовый пользователь";
				u.RoleId = createdRole?.Id;
				u.IsPasswordSet = true;
				createdUser = u;
			})
			.WithLicense(l =>
			{
				l.KeyCode = "TEST-1234-5678-90AB";
				l.IsActive = true;
				l.ExpiresAt = DateTime.UtcNow.AddDays(30);
				createdLicense = l;
			})
			.WithDevice(d =>
			{
				d.DeviceHardwareId = "test-device-123";
				d.DeviceName = "Test PC";
				d.LicenseId = createdLicense!.Id;
				createdDevice = d;
			})
			.WithRefreshToken(rt =>
			{
				rt.UserId = createdUser!.Id;
				rt.DeviceId = createdDevice!.Id;
				rt.Token = "test-refresh-token";
				rt.ExpiresAt = DateTime.UtcNow.AddDays(7);
				rt.IsRevoked = false;
				createdRefreshToken = rt;
			})
			.Build(customFactory.Services);

		// Act — другой device_id
		var response = await client.PostAsJsonAsync("/api/auth/refresh", new
		{
			refresh_token = "test-refresh-token",
			device_hardware_id = "different-device-456"
		});

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("Device mismatch");
	}

	// ====== Вспомогательные методы ======

	private WebApplicationFactory<Program> SetupTestFactory()
	{
		return _factory.WithWebHostBuilder(builder =>
		{
			builder.ConfigureServices(services =>
			{
				services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
				services.RemoveAll<DbContextOptions<AppDbContext>>();
				services.AddDbContext<AppDbContext>(options =>
					options.UseInMemoryDatabase("TestDb"));
				services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
			});
		});
	}

	private async Task<JsonElement> ParseJsonResponse(HttpResponseMessage response)
	{
		var content = await response.Content.ReadAsStringAsync();
		return JsonDocument.Parse(content).RootElement;
	}

}