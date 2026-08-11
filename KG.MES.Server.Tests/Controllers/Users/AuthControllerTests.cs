using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Tests.Helpers;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KG.MES.Server.Tests.Controllers.Users;

[Trait("Category", "Users")]
public class AuthControllerTests : TestBase
{
	public AuthControllerTests(WebApplicationFactory<Program> factory) : base(factory)
	{
	}

	[Fact]
	public async Task Login_WithValidCredentials_ShouldReturnTokens()
	{
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;

		BuildTestData(builder => builder
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
			}));

		// Устанавливаем пароль через PasswordHasher (не через TestDataBuilder, т.к. он не знает про хеширование)
		using (var scope = Services.CreateScope())
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

		var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

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
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;

		BuildTestData(builder => builder
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
			}));

		using (var scope = Services.CreateScope())
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

		var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("Invalid email or password");
	}

	[Fact]
	public async Task Login_WithExpiredLicense_ShouldReturnUnauthorized()
	{
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;

		BuildTestData(builder => builder
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
			}));

		using (var scope = Services.CreateScope())
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

		var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("License has expired");
	}

	[Fact]
	public async Task Refresh_WithValidToken_ShouldReturnNewAccessToken()
	{
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;
		Device? createdDevice = null;
		RefreshToken? createdRefreshToken = null;

		BuildTestData(builder => builder
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
			}));

		var request = new RefreshRequestDto
		{
			RefreshToken = "test-refresh-token",
			DeviceHardwareId = "test-device-123",
			LicenseKey = "TEST-1234-5678-90AB"
		};

		var response = await client.PostAsJsonAsync("/api/auth/refresh", request);

		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

		var json = await ParseJsonResponse(response);
		json.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
		json.GetProperty("token_type").GetString().Should().Be("Bearer");
		json.GetProperty("expires_in").GetInt32().Should().Be(300);
	}

	[Fact]
	public async Task Refresh_WithRevokedToken_ShouldReturnUnauthorized()
	{
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;
		Device? createdDevice = null;
		RefreshToken? createdRefreshToken = null;

		BuildTestData(builder => builder
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
			}));

		var response = await client.PostAsJsonAsync("/api/auth/refresh", new
		{
			refresh_token = "test-refresh-token",
			device_hardware_id = "test-device-123",
			license_key = "TEST-1234-5678-90AB"
		});

		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("Invalid refresh token");
	}

	[Fact]
	public async Task Refresh_WithDeviceMismatch_ShouldReturnUnauthorized()
	{
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;
		Device? createdDevice = null;
		RefreshToken? createdRefreshToken = null;

		BuildTestData(builder => builder
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
			}));

		var response = await client.PostAsJsonAsync("/api/auth/refresh", new
		{
			refresh_token = "test-refresh-token",
			device_hardware_id = "different-device-456",
			license_key = "TEST-1234-5678-90AB"
		});

		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("Device mismatch");
	}

	[Fact]
	public async Task Refresh_WithExpiredLicense_ShouldReturnUnauthorized()
	{
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;
		Device? createdDevice = null;
		RefreshToken? createdRefreshToken = null;

		BuildTestData(builder => builder
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
			}));

		var response = await client.PostAsJsonAsync("/api/auth/refresh", new
		{
			refresh_token = "test-refresh-token",
			device_hardware_id = "test-device-123",
			license_key = "EXPIRED-1234-5678-90AB"
		});

		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("License expired");
	}

	[Fact]
	public async Task Refresh_WithRevokedLicense_ShouldReturnUnauthorized()
	{
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;
		Device? createdDevice = null;
		RefreshToken? createdRefreshToken = null;

		BuildTestData(builder => builder
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
				l.KeyCode = "REVOKED-1234-5678-90AB";
				l.IsActive = false;  // ← отозвана
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
			}));

		var response = await client.PostAsJsonAsync("/api/auth/refresh", new
		{
			refresh_token = "test-refresh-token",
			device_hardware_id = "test-device-123",
			license_key = "REVOKED-1234-5678-90AB"
		});

		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("License is revoked");
	}

	[Fact]
	public async Task Refresh_WithDifferentLicenseKey_ShouldReturnUnauthorized()
	{
		var client = CreateClient();

		Role? createdRole = null;
		User? createdUser = null;
		License? createdLicense = null;
		Device? createdDevice = null;
		RefreshToken? createdRefreshToken = null;

		BuildTestData(builder => builder
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
			}));

		var response = await client.PostAsJsonAsync("/api/auth/refresh", new
		{
			refresh_token = "test-refresh-token",
			device_hardware_id = "test-device-123",
			license_key = "DIFFERENT-XXXX-XXXX-XXXX"
		});

		response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

		var json = await ParseJsonResponse(response);
		json.GetProperty("error").GetString().Should().Be("Invalid license key");
	}

	// ====== Вспомогательные методы ======

	private async Task<JsonElement> ParseJsonResponse(HttpResponseMessage response)
	{
		var content = await response.Content.ReadAsStringAsync();
		return JsonDocument.Parse(content).RootElement;
	}

}