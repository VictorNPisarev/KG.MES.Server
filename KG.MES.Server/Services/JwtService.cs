using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Server.Services.Models;
using Microsoft.IdentityModel.Tokens;

namespace KG.MES.Server.Services;

public class JwtService : IJwtService
{
	private readonly string _secret;
	private readonly string _issuer;
	private readonly string _audience;

	public JwtService(IConfiguration configuration)
	{
		_secret = configuration["Jwt:Secret"] ?? "super-secret-key-change-me-in-production";
		_issuer = configuration["Jwt:Issuer"] ?? "KG.MES.Server";
		_audience = configuration["Jwt:Audience"] ?? "KG.MES.Apps";
	}

	public string GenerateToken(Guid userId, string email, string role)
	{
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
			new Claim(ClaimTypes.Email, email),
			new Claim(ClaimTypes.Role, role),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _issuer,
			audience: _audience,
			claims: claims,
			expires: DateTime.UtcNow.AddHours(1),
			signingCredentials: creds
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	public string GenerateRefreshToken()
	{
		return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
	}


	public ClaimsPrincipal? ValidateToken(string token)
	{
		try
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.UTF8.GetBytes(_secret);

			var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = true,
				ValidIssuer = _issuer,
				ValidateAudience = true,
				ValidAudience = _audience,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			}, out _);

			return principal;
		}
		catch
		{
			return null;
		}
	}

	public string GenerateRegistrationToken(
		Guid licenseId,
		string email,
		string? applicationCode,
		string deviceHardwareId,
		string? deviceName)
	{
		var claims = new[]
		{
		new Claim("license_id", licenseId.ToString()),
		new Claim(ClaimTypes.Email, email),
		new Claim("application_code", applicationCode ?? string.Empty),
		new Claim("device_hardware_id", deviceHardwareId),        // ← новое
        new Claim("device_name", deviceName ?? string.Empty),      // ← новое
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
	};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _issuer,
			audience: _audience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(5),
			signingCredentials: creds
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	public RegistrationTokenPayload? ValidateRegistrationToken(string token)
	{
		try
		{
			var handler = new JwtSecurityTokenHandler();
			var key = Encoding.UTF8.GetBytes(_secret);

			var principal = handler.ValidateToken(token, new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = true,
				ValidIssuer = _issuer,
				ValidateAudience = true,
				ValidAudience = _audience,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			}, out _);

			var licenseIdClaim = principal.FindFirst("license_id")?.Value;
			var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
			var deviceHardwareIdClaim = principal.FindFirst("device_hardware_id")?.Value;

			if (string.IsNullOrEmpty(licenseIdClaim)
				|| string.IsNullOrEmpty(emailClaim)
				|| string.IsNullOrEmpty(deviceHardwareIdClaim))
				return null;

			return new RegistrationTokenPayload
			{
				LicenseId = Guid.Parse(licenseIdClaim),
				Email = emailClaim,
				ApplicationCode = principal.FindFirst("application_code")?.Value,
				DeviceHardwareId = deviceHardwareIdClaim,
				DeviceName = principal.FindFirst("device_name")?.Value
			};
		}
		catch
		{
			return null;
		}
	}
}