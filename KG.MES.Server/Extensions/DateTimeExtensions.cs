using System.Runtime.CompilerServices;

namespace KG.MES.Server.Extensions;

public static class DateTimeExtensions
{
	private static TimeZoneInfo? productionTimeZone;
	private static readonly ILogger? logger;

	public static void Initialize(IConfiguration configuration)
	{
		var timeZoneId = configuration["Production:TimeZoneId"] ?? "Russian Standard Time";
		productionTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
	}

	// Для тестов
	public static void SetTimeZone(TimeZoneInfo timeZone)
	{
		productionTimeZone = timeZone;
	}

	private static TimeZoneInfo GetTimeZone()
	{
		return productionTimeZone ?? TimeZoneInfo.Utc;
	}

	/// <summary>
	/// UTC в локальное время производства
	/// </summary>
	public static DateTime ToProductionTime(this DateTime utcDateTime)
	{
		// Если Kind не UTC — приводим к UTC
		if (utcDateTime.Kind != DateTimeKind.Utc)
		{
			utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
		}

		return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, GetTimeZone());
	}

	/// <summary>
	/// Локальное время производства в UTC
	/// </summary>
	public static DateTime ToUtcFromProduction(this DateTime productionLocalTime)
	{
		return TimeZoneInfo.ConvertTimeToUtc(productionLocalTime, GetTimeZone());
	}
}