using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;

namespace KG.MES.Shared.Services.Interfaces;

public interface IUserService
{
	Task<UserDto?> GetUserByEmailAsync(string email);
	Task<UserDto?> GetUserByIdAsync(Guid userId);
	Task<List<UserWorkplaceDto>> GetUserWorkplacesAsync(Guid userId);
}