using KG.MES.Server.Data;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public class UserService : IUserService
{
	private readonly AppDbContext _context;
	private readonly ILogger<UserService> _logger;

	public UserService(AppDbContext context, ILogger<UserService> logger)
	{
		_context = context;
		_logger = logger;
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