using System.Security.Claims;

namespace KG.MES.Server.Services.Interfaces;

public interface IJwtService
{
	string GenerateToken(Guid userId, string email, string role);
	string GenerateRefreshToken(); 
	ClaimsPrincipal? ValidateToken(string token);
}