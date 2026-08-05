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

		//user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
		//user.IsPasswordSet = true;
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
}