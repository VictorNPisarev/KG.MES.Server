using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;

namespace KG.MES.Server.Services.Interfaces;

public interface IUserService
{
	Task<User?> AuthenticateAsync(string email, string password);
	Task<bool> SetPasswordAsync(Guid userId, string newPassword);
	Task<UserDto?> GetUserByEmailAsync(string email);
	Task<UserDto?> GetUserByIdAsync(Guid userId);
	Task<List<UserWorkplaceDto>> GetUserWorkplacesAsync(Guid userId);
}