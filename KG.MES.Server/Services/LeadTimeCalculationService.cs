using KG.MES.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public class LeadTimeCalculationService
{
	private readonly AppDbContext _dbContext;
	private readonly ILogger<LeadTimeCalculationService> _logger;

	public LeadTimeCalculationService(
		AppDbContext dbContext,
		ILogger<LeadTimeCalculationService> logger)
	{
		_dbContext = dbContext;
		_logger = logger;
	}

	/// <summary>
	/// Рассчитывает дату окончания изготовления с учетом производственного календаря РФ.
	/// </summary>
	public async Task<DateTime> CalculateEndDateAsync(
		DateTime startDate,
		int workingDaysRequired,
		CancellationToken cancellationToken)
	{
		if (workingDaysRequired <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(workingDaysRequired));
		}

		var currentDate = startDate.Date;
		var daysAdded = 0;

		// Оптимизация: запрашиваем из БД диапазон с запасом (максимум 2 года вперед)
		var maxDate = currentDate.AddYears(2);

		var calendarDays = await _dbContext.ProductionCalendarDays
			.Where(d => d.CalendarDate > currentDate && d.CalendarDate <= maxDate)
			.OrderBy(d => d.CalendarDate)
			.ToListAsync(cancellationToken);

		if (!calendarDays.Any())
		{
			_logger.LogWarning("Производственный календарь пуст для даты {StartDate}", startDate);
			throw new InvalidOperationException("Данные производственного календаря отсутствуют");
		}

		foreach (var day in calendarDays)
		{
			if (day.IsWorkingDay)
			{
				daysAdded++;
				currentDate = day.CalendarDate;

				if (daysAdded >= workingDaysRequired)
				{
					break;
				}
			}
		}

		return currentDate;
	}
}