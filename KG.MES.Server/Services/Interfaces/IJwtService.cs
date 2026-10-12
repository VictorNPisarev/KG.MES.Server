using System.Security.Claims;
using KG.MES.Server.Services.Models;

namespace KG.MES.Server.Services.Interfaces;

public interface IJwtService
{
	string GenerateToken(Guid userId, string email, string role);
	string GenerateRefreshToken(); 
	ClaimsPrincipal? ValidateToken(string token);
	string GenerateRegistrationToken(Guid licenseId, string email, string? applicationCode, string deviceHardwareId, string? deviceName);
	RegistrationTokenPayload? ValidateRegistrationToken(string token);
}