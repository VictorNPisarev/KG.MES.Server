using System.Globalization;
using KG.MES.Server.Data;
using KG.MES.Server.Models.Dto;
using KG.MES.Server.Services;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductionCalendarController : ControllerBase
{
	private readonly AppDbContext _dbContext;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly LeadTimeCalculationService _calculationService;
	private readonly ILogger<ProductionCalendarController> _logger;

	public ProductionCalendarController(
		AppDbContext dbContext,
		IHttpClientFactory httpClientFactory,
		LeadTimeCalculationService calculationService,
		ILogger<ProductionCalendarController> logger)
	{
		_dbContext = dbContext;
		_httpClientFactory = httpClientFactory;
		_calculationService = calculationService;
		_logger = logger;
	}

	/// <summary>
	/// Синхронизирует календарь с isdayoff.ru (использовать только для начального заполнения или тестов).
	/// В продакшене лучше загружать JSON из официального Постановления Правительства.
	/// </summary>
	[HttpPost("sync/{year}")]
	public async Task<IActionResult> SyncCalendarAsync(
		int year,
		CancellationToken cancellationToken)
	{
		if (year < 2020 || year > 2030)
		{
			return BadRequest("Год должен быть в диапазоне 2020-2030");
		}

		var client = _httpClientFactory.CreateClient();
		var requestUrl = $"https://isdayoff.ru/api/getdata?year={year}&cc=ru&pre=1";

		var response = await client.GetStringAsync(requestUrl, cancellationToken);
		var daysData = response.Trim().ToCharArray();

		var startDate = new DateTime(year, 1, 1);
		var entitiesToUpsert = new List<ProductionCalendarDay>();

		for (int i = 0; i < daysData.Length; i++)
		{
			var currentDate = startDate.AddDays(i);
			var dayType = daysData[i];

			entitiesToUpsert.Add(new ProductionCalendarDay
			{
				CalendarDate = currentDate,
				IsWorkingDay = dayType == '0',
				IsShortenedDay = dayType == '2',
				Description = GetDayDescription(dayType)
			});
		}

		// Используем ExecuteUpdate / Upsert логику (в EF Core 7+ можно через NpgsqlSpecific)
		// Для простоты удаляем старые и вставляем новые (в рамках транзакции)
		using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

		await _dbContext.ProductionCalendarDays
			.Where(d => d.CalendarDate.Year == year)
			.ExecuteDeleteAsync(cancellationToken);

		await _dbContext.ProductionCalendarDays.AddRangeAsync(entitiesToUpsert, cancellationToken);
		await _dbContext.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		_logger.LogInformation("Календарь на {Year} год успешно синхронизирован", year);
		return Ok(new { message = $"Календарь на {year} год обновлен" });
	}

	[HttpPost("calculate")]
	public async Task<IActionResult> CalculateEndDateAsync(
		[FromBody] CalculateReadyDateRequestDto request,
		CancellationToken cancellationToken)
	{
		if (request == null)
		{
			return BadRequest(new { message = "Тело запроса пустое" });
		}

		// Явный парсинг даты с поддержкой нескольких форматов
		if (!TryParseDate(request.StartDate, out var startDate))
		{
			return BadRequest(new
			{
				message = $"Неверный формат даты: {request.StartDate}. " +
						  "Используйте формат: yyyy-MM-dd или yyyy-MM-ddTHH:mm:ss"
			});
		}

		try
		{
			var endDate = await _calculationService.CalculateEndDateAsync(
				startDate,
				request.WorkingDays,
				cancellationToken);

			var calendarDays = (endDate - startDate.Date).Days;

			var response = new CalculateReadyDateResponseDto
			{
				StartDate = startDate.Date,
				EndDate = endDate,
				WorkingDays = request.WorkingDays,
				CalendarDays = calendarDays
			};

			return Ok(response);
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogWarning(ex, "Ошибка расчета срока изготовления");
			return BadRequest(new { message = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Непредвиденная ошибка при расчете срока изготовления");
			return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
		}
	}

	/// <summary>
	/// Пытается распарсить дату из строки в различных форматах.
	/// </summary>
	private bool TryParseDate(string dateString, out DateTime result)
	{
		result = DateTime.MinValue;

		if (string.IsNullOrWhiteSpace(dateString))
		{
			return false;
		}

		// Массив поддерживаемых форматов
		string[] formats =
		{
				"yyyy-MM-dd",
				"yyyy-MM-ddTHH:mm:ss",
				"yyyy-MM-ddTHH:mm:ssZ",
				"yyyy-MM-ddTHH:mm:ss.fffZ",
				"dd.MM.yyyy",
				"dd/MM/yyyy"
			};

		return DateTime.TryParseExact(
			dateString,
			formats,
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out result);
	}


	private string GetDayDescription(char dayType)
	{
		return dayType switch
		{
			'0' => "Рабочий день",
			'1' => "Выходной или праздник",
			'2' => "Сокращенный предпраздничный день",
			_ => "Неизвестно"
		};
	}
}