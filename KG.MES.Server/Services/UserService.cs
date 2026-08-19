using KG.MES.Server.Data;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public class UserService : IUserService
{
	private readonly AppDbContext _context;
	private readonly IPasswordHasher<User> _passwordHasher;
	private readonly ILogger<UserService> _logger;

	public UserService(AppDbContext context, IPasswordHasher<User> passwordHasher, ILogger<UserService> logger)
	{
		_context = context;
		_passwordHasher = passwordHasher;
		_logger = logger;
	}

	public async Task<User?> AuthenticateAsync(string email, string password)
	{
		var user = await _context.Users
			.Include(u => u.Role)
			.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

		if (user == null || string.IsNullOrEmpty(user.PasswordHash))
			return null;

		var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

		if (result == PasswordVerificationResult.Success)
			return user;

		// Если хэш устарел (например, обновили алгоритм) — пересохраняем
		if (result == PasswordVerificationResult.SuccessRehashNeeded)
		{
			user.PasswordHash = _passwordHasher.HashPassword(user, password);
			await _context.SaveChangesAsync();
		}

		return null;
	}

	public async Task<bool> SetPasswordAsync(Guid userId, string newPassword)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null)
			return false;

		user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
		user.IsPasswordSet = true;
		await _context.SaveChangesAsync();

		return true;
	}

	public async Task<UserDto?> GetUserByEmailAsync(string email)
	{
		var user = await _context.Users
			.Include(u => u.Role)
			.FirstOrDefaultAsync(u => u.Email == email);

		if (user == null)
			return null;

		// Формируем DTO прямо в сервисе
		return new UserDto
		{
			Id = user.Id,
			Email = user.Email,
			Name = user.Name,
			RoleId = user.RoleId,
			RoleName = user.Role?.Name,
			RoleLevel = user.Role?.Level ?? 10
		};
	}

	public async Task<UserDto?> GetUserByIdAsync(Guid userId)
	{
		var user = await _context.Users
			.Include(u => u.Role)
			.FirstOrDefaultAsync(u => u.Id == userId);

		if (user == null)
			return null;

		// Формируем DTO прямо в сервисе
		return new UserDto
		{
			Id = user.Id,
			Email = user.Email,
			Name = user.Name,
			RoleId = user.RoleId,
			RoleName = user.Role?.Name,
			RoleLevel = user.Role?.Level ?? 10
		};
	}

	public async Task<List<UserWorkplaceDto>> GetUserWorkplacesAsync(Guid userId)
	{
		var user = await _context.Users
			.Include(u => u.Role)
			.FirstOrDefaultAsync(u => u.Id == userId);

		if (user == null)
			return new List<UserWorkplaceDto>();

		IQueryable<Workplace> query;

		if (user.Role?.Level >= 40)
		{
			query = _context.Workplaces.Where(w => w.IsWorkplace);
		}
		else
		{
			var workplaceIds = await _context.UserWorkplaces
				.Where(uw => uw.UserId == userId)
				.Select(uw => uw.WorkplaceId)
				.ToListAsync();

			query = _context.Workplaces.Where(w => workplaceIds.Contains(w.Id) && w.IsWorkplace);
		}

		return await query
			.OrderBy(w => w.Name)
			.Select(w => new UserWorkplaceDto
			{
				Id = w.Id,
				Name = w.Name,
				PreviousWorkplaceId = w.PreviousWorkplaceId,
				IsWorkplace = w.IsWorkplace
			})
			.ToListAsync();
	}

	/// <summary>
	/// Получить список всех пользователей с пагинацией и поиском
	/// </summary>
	public async Task<PaginatedResponse<UserAdminListItemDto>> GetAllUsersAsync(
		int page,
		int limit,
		string? search)
	{
		var query = _context.Users
			.Include(u => u.Role)
			.AsQueryable();

		// Поиск по email или имени
		if (!string.IsNullOrEmpty(search))
		{
			query = query.Where(u =>
				EF.Functions.ILike(u.Email, $"%{search}%") ||
				(u.Name != null && EF.Functions.ILike(u.Name, $"%{search}%")));
		}

		var total = await query.CountAsync();

		var items = await query
			.OrderBy(u => u.Email)
			.Skip((page - 1) * limit)
			.Take(limit)
			.Select(u => new UserAdminListItemDto
			{
				Id = u.Id,
				Email = u.Email,
				Name = u.Name,
				RoleName = u.Role != null ? u.Role.Name : null,
				IsActive = u.IsActive,
				IsPasswordSet = u.IsPasswordSet,
				CreatedAt = u.CreatedAt//,
				//LastLoginAt = u.LastLoginAt
			})
			.ToListAsync();

		return new PaginatedResponse<UserAdminListItemDto>
		{
			Data = items,
			Pagination = new PaginationInfo
			{
				Page = page,
				Limit = limit,
				Total = total,
				Pages = (int)Math.Ceiling(total / (double)limit)
			}
		};
	}

	/// <summary>
	/// Получить детальную информацию о пользователе
	/// </summary>
	public async Task<UserAdminDetailsDto?> GetUserDetailsAsync(Guid userId)
	{
		var user = await _context.Users
			.Include(u => u.Role)
			.Include(u => u.UserWorkplaces!)
				.ThenInclude(uw => uw.Workplace)
			.FirstOrDefaultAsync(u => u.Id == userId);

		if (user == null)
			return null;

		return new UserAdminDetailsDto
		{
			Id = user.Id,
			Email = user.Email,
			Name = user.Name,
			RoleName = user.Role?.Name,
			RoleLevel = user.Role?.Level,
			IsActive = user.IsActive,
			IsPasswordSet = user.IsPasswordSet,
			CreatedAt = user.CreatedAt,
			//LastLoginAt = user.LastLoginAt,
			Workplaces = user.UserWorkplaces?
				.Select(uw => new UserWorkplaceDto
				{
					Id = uw.WorkplaceId,
					Name = uw.Workplace != null ? uw.Workplace.Name : string.Empty
				})
				.ToList() ?? new()
		};
	}

	/// <summary>
	/// Создать нового пользователя
	/// </summary>
	public async Task<CreateUserResultDto> CreateUserAsync(CreateUserRequestDto request)
	{
		// Проверяем, не существует ли уже пользователь с таким email
		var existingUser = await _context.Users
			.FirstOrDefaultAsync(u => u.Email == request.Email);

		if (existingUser != null)
		{
			return new CreateUserResultDto
			{
				Success = false,
				Error = "Пользователь с таким email уже существует"
			};
		}

		// Находим роль
		Role? role = null;
		if (!string.IsNullOrEmpty(request.RoleName))
		{
			role = await _context.Roles
				.FirstOrDefaultAsync(r => r.Name == request.RoleName);
		}

		// Если роль не найдена — используем дефолтную
		if (role == null)
		{
			role = await _context.Roles
				.FirstOrDefaultAsync(r => r.Name == "User")
				?? new Role { Id = Guid.NewGuid(), Name = "User", Level = 10 };
		}

		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = request.Email,
			Name = request.Name ?? request.Email,
			RoleId = role.Id,
			IsActive = true,
			IsPasswordSet = false,
			CreatedAt = DateTime.UtcNow
		};

		// Если пароль передан — хешируем
		if (!string.IsNullOrEmpty(request.Password))
		{
			user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
			user.IsPasswordSet = true;
		}

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_logger.LogInformation("User {Email} created by admin", user.Email);

		return new CreateUserResultDto
		{
			Success = true,
			User = new UserAdminListItemDto
			{
				Id = user.Id,
				Email = user.Email,
				Name = user.Name,
				RoleName = role.Name,
				IsActive = user.IsActive,
				IsPasswordSet = user.IsPasswordSet,
				CreatedAt = user.CreatedAt
			}
		};
	}

	/// <summary>
	/// Заблокировать пользователя
	/// </summary>
	public async Task<bool> BlockUserAsync(Guid userId)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null)
			return false;

		user.IsActive = false;
		await _context.SaveChangesAsync();

		_logger.LogInformation("User {Email} blocked by admin", user.Email);
		return true;
	}

	/// <summary>
	/// Разблокировать пользователя
	/// </summary>
	public async Task<bool> UnblockUserAsync(Guid userId)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null)
			return false;

		user.IsActive = true;
		await _context.SaveChangesAsync();

		_logger.LogInformation("User {Email} unblocked by admin", user.Email);
		return true;
	}

	/// <summary>
	/// Сбросить пароль пользователя (генерирует новый)
	/// </summary>
	public async Task<ResetPasswordResultDto> ResetPasswordAsync(Guid userId)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null)
		{
			return new ResetPasswordResultDto
			{
				Success = false,
				Error = "Пользователь не найден"
			};
		}

		var newPassword = GenerateRandomPassword();
		user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
		user.IsPasswordSet = true;
		await _context.SaveChangesAsync();

		_logger.LogInformation("Password reset for user {Email}", user.Email);

		return new ResetPasswordResultDto
		{
			Success = true,
			NewPassword = newPassword
		};
	}

	/// <summary>
	/// Назначить роль пользователю
	/// </summary>
	public async Task<bool> SetUserRoleAsync(Guid userId, string roleName)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null)
			return false;

		var role = await _context.Roles
			.FirstOrDefaultAsync(r => r.Name == roleName);
		if (role == null)
			return false;

		user.RoleId = role.Id;
		await _context.SaveChangesAsync();

		_logger.LogInformation("User {Email} role changed to {Role}", user.Email, roleName);
		return true;
	}

	/// <summary>
	/// Генерация случайного пароля
	/// </summary>
	private static string GenerateRandomPassword()
	{
		const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%";
		var random = Random.Shared;
		var length = 12;
		var password = new char[length];

		// Гарантируем хотя бы одну цифру, одну букву и один спецсимвол
		password[0] = chars[random.Next(26)]; // буква
		password[1] = chars[26 + random.Next(8)]; // цифра (первые 8 символов после букв)
		password[2] = chars[34 + random.Next(4)]; // спецсимвол

		for (int i = 3; i < length; i++)
		{
			password[i] = chars[random.Next(chars.Length)];
		}

		// Перемешиваем
		return new string(password.OrderBy(_ => random.Next()).ToArray());
	}
}