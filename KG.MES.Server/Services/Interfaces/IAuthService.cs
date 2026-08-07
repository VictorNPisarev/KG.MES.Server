using KG.MES.Server.Models.Dto;
using KG.MES.Shared.Models.Dto;

namespace KG.MES.Server.Services.Interfaces;

public interface IAuthService
{
	Task<LoginResultDto> AuthenticateUserAsync(LoginRequestDto request, string? ipAddress = null);
	Task<LoginResultDto> RefreshAuthenticationToken(RefreshRequestDto request);
}