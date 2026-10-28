using KG.MES.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly ILogger<HealthController> _logger;

	public HealthController(AppDbContext context, ILogger<HealthController> logger)
	{
		_context = context;
		_logger = logger;
	}

	/// <summary>
	/// Полная диагностика сервера: БД, подключение, тестовый запрос
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> GetHealth()
	{
		var results = new
		{
			timestamp = DateTime.UtcNow,
			server = new
			{
				status = "running",
				environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
			},
			database = new
			{
				status = "unknown",
				connectionString = GetMaskedConnectionString(),
				canConnect = false,
				testQuery = false,
				error = (string?)null
			},
			services = new
			{
				signalR = "running"
			},
			endpoints = new List<object>()
		};

		var databaseStatus = new
		{
			status = "unknown",
			connectionString = GetMaskedConnectionString(),
			canConnect = false,
			testQuery = false,
			error = (string?)null
		};

		try
		{
			// 1. Проверка подключения к БД
			await _context.Database.OpenConnectionAsync();
			databaseStatus = databaseStatus with { canConnect = true };

			// 2. Простой тестовый запрос
			var testResult = await _context.Workplaces
				.Select(w => new { w.Id, w.Name })
				.FirstOrDefaultAsync();

			var testQueryStatus = testResult != null;
			databaseStatus = databaseStatus with { testQuery = testResult != null };

			if (testResult != null)
			{
				databaseStatus = databaseStatus with
				{
					status = "healthy",
					testQuery = true,
					error = null
				};
			}
			else
			{
				databaseStatus = databaseStatus with
				{
					status = "warning",
					testQuery = false,
					error = "Таблица Workplaces пуста или недоступна"
				};
			}
		}
		catch (Exception ex)
		{
			databaseStatus = databaseStatus with
			{
				status = "unhealthy",
				error = ex.Message,
				canConnect = false,
				testQuery = false
			};
			_logger.LogError(ex, "Health check failed");
		}
		finally
		{
			await _context.Database.CloseConnectionAsync();
		}

		return Ok(new
		{
			timestamp = DateTime.UtcNow,
			server = new
			{
				status = "running",
				environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
				processId = Environment.ProcessId,
				machineName = Environment.MachineName
			},
			database = databaseStatus,
			services = new
			{
				signalR = "running"
			}
		});
	}

	/// <summary>
	/// Базовый ping для проверки доступности сервера
	/// </summary>
	[HttpGet("ping")]
	public IActionResult Ping()
	{
		return Ok(new
		{
			status = "ok",
			timestamp = DateTime.UtcNow,
			server = Environment.MachineName,
			version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
		});
	}

	/// <summary>
	/// Простой тест БД без нагрузки
	/// </summary>
	[HttpGet("db")]
	public async Task<IActionResult> TestDb()
	{
		try
		{
			var result = await _context.Database
				.ExecuteSqlRawAsync("SELECT 1");

			return Ok(new
			{
				status = "ok",
				message = "Database connection successful",
				result = result
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				status = "error",
				message = "Database connection failed",
				error = ex.Message
			});
		}
	}

	private string GetMaskedConnectionString()
	{
		var connString = _context.Database.GetConnectionString() ?? "not set";
		// Маскируем пароль
		var regex = new System.Text.RegularExpressions.Regex("Password=[^;]+");
		return regex.Replace(connString, "Password=***");
	}
}