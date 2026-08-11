using KG.MES.Server.Data;
using KG.MES.Server.Services;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KG.MES.Server.Tests.Helpers;

/// <summary>
/// Базовый класс для всех тестов, обеспечивающий изолированную InMemory БД для каждого теста
/// </summary>
public abstract class TestBase : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
	private readonly WebApplicationFactory<Program> _factory;
	private WebApplicationFactory<Program>? _currentFactory;
	private readonly string _dbName;
	private bool _disposed;

	protected TestBase(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
		_dbName = $"TestDb_{Guid.NewGuid():N}";
	}

	/// <summary>
	/// HTTP клиент с изолированной БД для текущего теста
	/// </summary>
	protected HttpClient CreateClient(bool configureJwt = false)
	{
		_currentFactory = _factory.WithWebHostBuilder(builder =>
		{
			builder.ConfigureServices(services =>
			{
				// Удаляем старые конфигурации DbContext
				services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
				services.RemoveAll<DbContextOptions<AppDbContext>>();

				// Регистрируем InMemory БД с уникальным именем
				services.AddDbContext<AppDbContext>(options =>
				{
					options.UseInMemoryDatabase(_dbName);
					options.ConfigureWarnings(warnings =>
						warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
				});

				//Сервисы для тестов
				services.AddScoped<IUserService, UserService>();
				services.AddScoped<IOrderService, OrderService>();
				services.AddScoped<ISupplyService, SupplyService>();
				services.AddScoped<IWorkplaceService, WorkplaceService>();
				services.AddScoped<IAuthService, AuthService>();
				services.AddScoped<IJwtService, JwtService>();
				services.AddScoped<ILicenseService, LicenseService>();
				services.AddScoped<IUserDeviceService, UserDeviceService>();
				services.AddScoped<OrderAttributeService>();
				services.AddScoped<LeadTimeCalculationService>();
				services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
				services.AddHttpClient();
				services.AddSignalR();
				 services.AddLogging();

			});

			// Добавляем JWT конфигурацию если нужно
			if (configureJwt)
			{
				builder.ConfigureAppConfiguration((context, config) =>
				{
					config.AddInMemoryCollection(new Dictionary<string, string?>
					{
						["Jwt:Secret"] = "test-secret-key-for-unit-tests-only-minimum-32-chars",
						["Jwt:Issuer"] = "KG.MES.Server.Tests",
						["Jwt:Audience"] = "KG.MES.Apps.Tests"
					});
				});
			}

		});

		return _currentFactory.CreateClient();
	}

	/// <summary>
	/// ServiceProvider для доступа к сервисам
	/// </summary>
	protected IServiceProvider Services => _currentFactory?.Services
		?? throw new InvalidOperationException("CreateClient() must be called before accessing Services");

	/// <summary>
	/// DbContext для прямого доступа к БД (для проверок)
	/// </summary>
	protected async Task<AppDbContext> GetDbContextAsync()
	{
		if (_currentFactory == null)
			throw new InvalidOperationException("CreateClient() must be called before accessing DbContext");

		var scope = _currentFactory.Services.CreateAsyncScope();
		return scope.ServiceProvider.GetRequiredService<AppDbContext>();
	}

	/// <summary>
	/// Создает и заполняет БД тестовыми данными через TestDataBuilder
	/// </summary>
	protected void BuildTestData(Action<TestDataBuilder> configure)
	{
		if (_currentFactory == null)
			throw new InvalidOperationException("CreateClient() must be called before building test data");

		var builder = new TestDataBuilder();
		configure(builder);
		builder.Build(_currentFactory.Services);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
		{
			_currentFactory?.Dispose();
		}

		_disposed = true;
	}
}