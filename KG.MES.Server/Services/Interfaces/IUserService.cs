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


	Task<PaginatedResponse<UserAdminListItemDto>> GetAllUsersAsync(
	int page, int limit, string? search);

	Task<UserAdminDetailsDto?> GetUserDetailsAsync(Guid userId);

	Task<CreateUserResultDto> CreateUserAsync(CreateUserRequestDto request);

	Task<bool> BlockUserAsync(Guid userId);

	Task<bool> UnblockUserAsync(Guid userId);

	Task<ResetPasswordResultDto> ResetPasswordAsync(Guid userId);

	Task<bool> SetUserRoleAsync(Guid userId, string roleName);

}