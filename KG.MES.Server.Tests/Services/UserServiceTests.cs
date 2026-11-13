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

	// ====== GetManagersAsync / SearchUsersAsync ======

	[Fact]
	public async Task GetManagersAsync_ShouldReturnOnlySalesManagers()
	{
		// Arrange
		var salesRole = new Role { Id = Guid.NewGuid(), Code = "SALES_MANAGER", Name = "Sales manager", Level = 10 };
		var masterRole = new Role { Id = Guid.NewGuid(), Code = "MASTER", Name = "Master", Level = 10 };

		var salesUser1 = new User { Id = Guid.NewGuid(), Email = "sm1@test.com", Name = "Анна Менеджерова", RoleId = salesRole.Id, IsActive = true };
		var salesUser2 = new User { Id = Guid.NewGuid(), Email = "sm2@test.com", Name = "Борис Продавцов", RoleId = salesRole.Id, IsActive = true };
		var masterUser = new User { Id = Guid.NewGuid(), Email = "master@test.com", Name = "Виктор Мастеров", RoleId = masterRole.Id, IsActive = true };

		_context.Roles.AddRange(salesRole, masterRole);
		_context.Users.AddRange(salesUser1, salesUser2, masterUser);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetManagersAsync();

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain(m => m.Id == salesUser1.Id);
		result.Should().Contain(m => m.Id == salesUser2.Id);
		result.Should().NotContain(m => m.Id == masterUser.Id);

		// Сортировка по Name
		result[0].Name.Should().Be("Анна Менеджерова");
		result[1].Name.Should().Be("Борис Продавцов");
	}

	[Fact]
	public async Task GetManagersAsync_WithSearch_ShouldFilterByNameOrEmail()
	{
		// Arrange
		var salesRole = new Role { Id = Guid.NewGuid(), Code = "SALES_MANAGER", Name = "Sales manager", Level = 10 };

		_context.Roles.Add(salesRole);
		_context.Users.AddRange(
			new User { Id = Guid.NewGuid(), Email = "anna@company.ru", Name = "Анна Смирнова", RoleId = salesRole.Id, IsActive = true },
			new User { Id = Guid.NewGuid(), Email = "boris@other.ru", Name = "Борис Иванов", RoleId = salesRole.Id, IsActive = true },
			new User { Id = Guid.NewGuid(), Email = "anna.ivanova@company.ru", Name = "Анна Иванова", RoleId = salesRole.Id, IsActive = true });
		await _context.SaveChangesAsync();

		// Act — по имени
		var byName = await _service.GetManagersAsync("анна");

		// Assert — case-insensitive, 2 записи
		byName.Should().HaveCount(2);
		byName.Should().OnlyContain(m => m.Name.Contains("Анна"));

		// Act — по email
		var byEmail = await _service.GetManagersAsync("@company.ru");

		// Assert
		byEmail.Should().HaveCount(2);
		byEmail.Should().OnlyContain(m => m.Email.Contains("@company.ru"));
	}

	[Fact]
	public async Task GetManagersAsync_ShouldExcludeInactiveUsers()
	{
		// Arrange
		var salesRole = new Role { Id = Guid.NewGuid(), Code = "SALES_MANAGER", Name = "Sales manager", Level = 10 };

		_context.Roles.Add(salesRole);
		_context.Users.AddRange(
			new User { Id = Guid.NewGuid(), Email = "active@test.com", Name = "Активный", RoleId = salesRole.Id, IsActive = true },
			new User { Id = Guid.NewGuid(), Email = "blocked@test.com", Name = "Заблокированный", RoleId = salesRole.Id, IsActive = false });
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetManagersAsync();

		// Assert — только активный
		result.Should().HaveCount(1);
		result[0].Name.Should().Be("Активный");
	}

	[Fact]
	public async Task SearchUsersAsync_ShouldReturnAllUsers()
	{
		// Arrange
		var salesRole = new Role { Id = Guid.NewGuid(), Code = "SALES_MANAGER", Name = "Sales manager", Level = 10 };
		var masterRole = new Role { Id = Guid.NewGuid(), Code = "MASTER", Name = "Master", Level = 10 };

		_context.Roles.AddRange(salesRole, masterRole);
		_context.Users.AddRange(
			new User { Id = Guid.NewGuid(), Email = "user1@test.com", Name = "Альфа", RoleId = salesRole.Id, IsActive = true },
			new User { Id = Guid.NewGuid(), Email = "user2@test.com", Name = "Бета", RoleId = masterRole.Id, IsActive = true });
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.SearchUsersAsync();

		// Assert — все пользователи, независимо от роли
		result.Should().HaveCount(2);
		result.Select(u => u.Name).Should().BeInAscendingOrder();
	}

	[Fact]
	public async Task SearchUsersAsync_WithSearch_ShouldFilter()
	{
		// Arrange
		_context.Users.AddRange(
			new User { Id = Guid.NewGuid(), Email = "alice@company.ru", Name = "Алиса", IsActive = true },
			new User { Id = Guid.NewGuid(), Email = "bob@other.ru", Name = "Боб", IsActive = true },
			new User { Id = Guid.NewGuid(), Email = "charlie@company.ru", Name = "Чарли", IsActive = true });
		await _context.SaveChangesAsync();

		// Act — по email
		var byEmail = await _service.SearchUsersAsync("company.ru");

		// Assert
		byEmail.Should().HaveCount(2);
		byEmail.Should().OnlyContain(u => u.Email.Contains("company.ru"));

		// Act — по имени
		var byName = await _service.SearchUsersAsync("боб");

		// Assert
		byName.Should().HaveCount(1);
		byName[0].Name.Should().Be("Боб");
	}

	[Fact]
	public async Task SearchUsersAsync_ShouldRespectLimit20()
	{
		// Arrange — создаём 25 пользователей
		for (int i = 1; i <= 25; i++)
		{
			_context.Users.Add(new User
			{
				Id = Guid.NewGuid(),
				Email = $"user{i:D2}@test.com",
				Name = $"Пользователь {i:D2}",
				IsActive = true
			});
		}
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.SearchUsersAsync();

		// Assert
		result.Should().HaveCount(20);
	}
}