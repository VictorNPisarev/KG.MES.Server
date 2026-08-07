using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KG.MES.Server.Services.Interfaces;
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
}