using FluentAssertions;
using KG.MES.Server.Data;
using KG.MES.Server.Services;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KG.MES.Server.Tests.Services;

[Trait("Category", "Unit")]
public class UserServiceTests : IDisposable
{
	private readonly AppDbContext _context;
	private readonly UserService _service;
	private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
	private readonly Mock<ILogger<UserService>> _loggerMock;

	public UserServiceTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}")
			.Options;

		_context = new AppDbContext(options);
		_passwordHasherMock = new Mock<IPasswordHasher<User>>();
		_loggerMock = new Mock<ILogger<UserService>>();

		_service = new UserService(_context, _passwordHasherMock.Object, _loggerMock.Object);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnUser()
	{
		// Arrange
		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = "test@example.com",
			Name = "Test User",
			PasswordHash = "hashed_password",
			IsActive = true
		};
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_passwordHasherMock
			.Setup(h => h.VerifyHashedPassword(user, "hashed_password", "Qwerty123!"))
			.Returns(PasswordVerificationResult.Success);

		// Act
		var result = await _service.AuthenticateAsync("test@example.com", "Qwerty123!");

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(user.Id);
		result.Email.Should().Be("test@example.com");
	}

	[Fact]
	public async Task AuthenticateAsync_WithInvalidPassword_ShouldReturnNull()
	{
		// Arrange
		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = "test@example.com",
			PasswordHash = "hashed_password",
			IsActive = true
		};
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_passwordHasherMock
			.Setup(h => h.VerifyHashedPassword(user, "hashed_password", "WrongPassword!"))
			.Returns(PasswordVerificationResult.Failed);

		// Act
		var result = await _service.AuthenticateAsync("test@example.com", "WrongPassword!");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task AuthenticateAsync_WithNonExistentUser_ShouldReturnNull()
	{
		// Act
		var result = await _service.AuthenticateAsync("nonexistent@example.com", "password");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task AuthenticateAsync_WithInactiveUser_ShouldReturnNull()
	{
		// Arrange
		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = "inactive@example.com",
			PasswordHash = "hashed_password",
			IsActive = false
		};
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.AuthenticateAsync("inactive@example.com", "password");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task SetPasswordAsync_ShouldHashAndSavePassword()
	{
		// Arrange
		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = "test@example.com",
			IsPasswordSet = false
		};
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_passwordHasherMock
			.Setup(h => h.HashPassword(user, "NewPassword123!"))
			.Returns("hashed_password");

		// Act
		var result = await _service.SetPasswordAsync(user.Id, "NewPassword123!");

		// Assert
		result.Should().BeTrue();

		var updatedUser = await _context.Users.FindAsync(user.Id);
		updatedUser.Should().NotBeNull();
		updatedUser!.PasswordHash.Should().Be("hashed_password");
		updatedUser.IsPasswordSet.Should().BeTrue();
	}

	[Fact]
	public async Task SetPasswordAsync_WithNonExistentUser_ShouldReturnFalse()
	{
		// Act
		var result = await _service.SetPasswordAsync(Guid.NewGuid(), "password");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task GetUserByEmailAsync_ShouldReturnUserDto()
	{
		// Arrange
		var role = new Role { Id = Guid.NewGuid(), Name = "Admin", Level = 50 };
		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = "test@example.com",
			Name = "Test User",
			RoleId = role.Id,
			IsActive = true
		};
		_context.Roles.Add(role);
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetUserByEmailAsync("test@example.com");

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(user.Id);
		result.Email.Should().Be("test@example.com");
		result.Name.Should().Be("Test User");
		result.RoleId.Should().Be(role.Id);
		result.RoleName.Should().Be("Admin");
		result.RoleLevel.Should().Be(50);
	}

	[Fact]
	public async Task GetUserByEmailAsync_WithNonExistentUser_ShouldReturnNull()
	{
		// Act
		var result = await _service.GetUserByEmailAsync("nonexistent@example.com");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetUserWorkplacesAsync_ForAdmin_ShouldReturnAllWorkplaces()
	{
		// Arrange
		var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin", Level = 50 };
		var admin = new User
		{
			Id = Guid.NewGuid(),
			Email = "admin@example.com",
			RoleId = adminRole.Id
		};

		var workplace1 = new Workplace { Id = Guid.NewGuid(), Name = "Workplace 1", IsWorkplace = true };
		var workplace2 = new Workplace { Id = Guid.NewGuid(), Name = "Workplace 2", IsWorkplace = true };
		var nonWorkplace = new Workplace { Id = Guid.NewGuid(), Name = "none", IsWorkplace = false };

		_context.Roles.Add(adminRole);
		_context.Users.Add(admin);
		_context.Workplaces.AddRange(workplace1, workplace2, nonWorkplace);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetUserWorkplacesAsync(admin.Id);

		// Assert
		result.Should().HaveCount(2);
		result.Should().NotContain(w => w.Id == nonWorkplace.Id);
	}

	[Fact]
	public async Task GetUserWorkplacesAsync_ForRegularUser_ShouldReturnAssignedWorkplaces()
	{
		// Arrange
		var role = new Role { Id = Guid.NewGuid(), Name = "User", Level = 10 };
		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = "user@example.com",
			RoleId = role.Id
		};

		var workplace1 = new Workplace { Id = Guid.NewGuid(), Name = "Workplace 1", IsWorkplace = true };
		var workplace2 = new Workplace { Id = Guid.NewGuid(), Name = "Workplace 2", IsWorkplace = true };
		var workplace3 = new Workplace { Id = Guid.NewGuid(), Name = "Workplace 3", IsWorkplace = true };

		_context.Roles.Add(role);
		_context.Users.Add(user);
		_context.Workplaces.AddRange(workplace1, workplace2, workplace3);
		_context.UserWorkplaces.AddRange(
			new UserWorkplace { UserId = user.Id, WorkplaceId = workplace1.Id },
			new UserWorkplace { UserId = user.Id, WorkplaceId = workplace2.Id }
		);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetUserWorkplacesAsync(user.Id);

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain(w => w.Id == workplace1.Id);
		result.Should().Contain(w => w.Id == workplace2.Id);
		result.Should().NotContain(w => w.Id == workplace3.Id);
	}

	[Fact]
	public async Task GetUserWorkplacesAsync_WithNonExistentUser_ShouldReturnEmptyList()
	{
		// Act
		var result = await _service.GetUserWorkplacesAsync(Guid.NewGuid());

		// Assert
		result.Should().BeEmpty();
	}
}