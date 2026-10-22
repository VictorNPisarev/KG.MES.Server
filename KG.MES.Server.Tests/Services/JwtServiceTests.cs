using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using KG.MES.Server.Services;
using KG.MES.Server.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace KG.MES.Server.Tests.Services;

[Trait("Category", "Unit")]
public class JwtServiceTests
{
	private readonly IJwtService _jwtService;
	private readonly IConfiguration _configuration;

	public JwtServiceTests()
	{
		// Создаем конфигурацию для тестов
		var configDict = new Dictionary<string, string?>
		{
			["Jwt:Secret"] = "test-secret-key-for-unit-tests-only-minimum-32-chars",
			["Jwt:Issuer"] = "TestIssuer",
			["Jwt:Audience"] = "TestAudience"
		};

		_configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(configDict)
			.Build();

		_jwtService = new JwtService(_configuration);
	}

	[Fact]
	public void GenerateToken_ShouldReturnValidJwtToken()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var email = "test@example.com";
		var role = "Admin";

		// Act
		var token = _jwtService.GenerateToken(userId, email, role);

		// Assert
		token.Should().NotBeNullOrEmpty();

		// Проверяем, что токен можно распарсить
		var handler = new JwtSecurityTokenHandler();
		var jsonToken = handler.ReadJwtToken(token);

		jsonToken.Should().NotBeNull();
		jsonToken.Issuer.Should().Be("TestIssuer");
		jsonToken.Audiences.Should().Contain("TestAudience");

		// Проверяем claims
		jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
		jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == email);
		jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
	}

	[Fact]
	public void GenerateToken_WithDifferentRoles_ShouldIncludeCorrectRoleClaim()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var email = "user@example.com";

		// Act
		var token = _jwtService.GenerateToken(userId, email, "Manager");
		var handler = new JwtSecurityTokenHandler();
		var jsonToken = handler.ReadJwtToken(token);

		// Assert
		jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Manager");
		jsonToken.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
	}

	[Fact]
	public void GenerateRefreshToken_ShouldReturnRandomString()
	{
		// Act
		var token1 = _jwtService.GenerateRefreshToken();
		var token2 = _jwtService.GenerateRefreshToken();

		// Assert
		token1.Should().NotBeNullOrEmpty();
		token2.Should().NotBeNullOrEmpty();

		// Токены должны быть разными (случайными)
		token1.Should().NotBe(token2);

		// Проверяем, что это Base64 строка
		var bytes = Convert.FromBase64String(token1);
		bytes.Length.Should().Be(64); // 64 байта = 512 бит
	}

	[Fact]
	public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var email = "test@example.com";
		var role = "User";

		var token = _jwtService.GenerateToken(userId, email, role);

		// Act
		var principal = _jwtService.ValidateToken(token);

		// Assert
		principal.Should().NotBeNull();

		var nameIdentifier = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		nameIdentifier.Should().Be(userId.ToString());

		var emailClaim = principal?.FindFirst(ClaimTypes.Email)?.Value;
		emailClaim.Should().Be(email);

		var roleClaim = principal?.FindFirst(ClaimTypes.Role)?.Value;
		roleClaim.Should().Be(role);
	}

	[Fact]
	public void ValidateToken_WithInvalidToken_ShouldReturnNull()
	{
		// Arrange
		var invalidToken = "this.is.not.a.valid.token";

		// Act
		var principal = _jwtService.ValidateToken(invalidToken);

		// Assert
		principal.Should().BeNull();
	}

	[Fact]
	public void ValidateToken_WithTamperedToken_ShouldReturnNull()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var email = "test@example.com";
		var role = "User";

		var token = _jwtService.GenerateToken(userId, email, role);

		// Tamper: меняем последний символ
		var tamperedToken = token.Substring(0, token.Length - 1) + "X";

		// Act
		var principal = _jwtService.ValidateToken(tamperedToken);

		// Assert
		principal.Should().BeNull();
	}

	[Fact]
	public void GenerateToken_ShouldHaveCorrectExpirationTime()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var email = "test@example.com";
		var role = "User";

		// Act
		var token = _jwtService.GenerateToken(userId, email, role);
		var handler = new JwtSecurityTokenHandler();
		var jsonToken = handler.ReadJwtToken(token);

		// Assert
		// В JwtService установлено expiration = DateTime.UtcNow.AddHours(1)
		var expectedExpiry = DateTime.UtcNow.AddHours(1);
		jsonToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void GenerateToken_WithNullConfiguration_ShouldUseDefaultValues()
	{
		// Arrange
		var emptyConfig = new ConfigurationBuilder().Build();
		var service = new JwtService(emptyConfig);

		var userId = Guid.NewGuid();
		var email = "test@example.com";
		var role = "User";

		// Act
		var token = service.GenerateToken(userId, email, role);

		// Assert
		token.Should().NotBeNullOrEmpty();

		var handler = new JwtSecurityTokenHandler();
		var jsonToken = handler.ReadJwtToken(token);

		// Проверяем дефолтные значения
		jsonToken.Issuer.Should().Be("KG.MES.Server");
		jsonToken.Audiences.Should().Contain("KG.MES.Apps");
	}
}