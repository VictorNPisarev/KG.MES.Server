namespace KG.MES.Main.Services
{
	public class HolidayCalendarService
	{
		private readonly HashSet<DateTime> _holidays = new();

		public HolidayCalendarService()
		{
			// Загружаем праздники (можно из JSON или Google Calendar)
			LoadHolidays();
		}

		private void LoadHolidays()
		{
			// Статические праздники России
			var staticHolidays = new[]
			{
				new DateTime(DateTime.Now.Year, 1, 1),  // Новый год
				new DateTime(DateTime.Now.Year, 1, 2),
				new DateTime(DateTime.Now.Year, 1, 3),
				new DateTime(DateTime.Now.Year, 1, 4),
				new DateTime(DateTime.Now.Year, 1, 5),
				new DateTime(DateTime.Now.Year, 1, 6),
				new DateTime(DateTime.Now.Year, 1, 7),  // Рождество
				new DateTime(DateTime.Now.Year, 1, 8),
				new DateTime(DateTime.Now.Year, 2, 23), // День защитника Отечества
				new DateTime(DateTime.Now.Year, 3, 8),  // Международный женский день
				new DateTime(DateTime.Now.Year, 5, 1),  // Праздник Весны и Труда
				new DateTime(DateTime.Now.Year, 5, 9),  // День Победы
				new DateTime(DateTime.Now.Year, 6, 12), // День России
				new DateTime(DateTime.Now.Year, 11, 4), // День народного единства
			};

			foreach (var holiday in staticHolidays)
				_holidays.Add(holiday);
		}

		/*public async Task LoadFromGoogleCalendarAsync(string calendarId, string apiKey)
		{
			// TODO: загрузка праздников из Google Calendar API
			// https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events?key={apiKey}
		}*/

		public DateTime AddBusinessDays(DateTime startDate, int days)
		{
			var result = startDate;
			var daysAdded = 0;

			while (daysAdded < days)
			{
				result = result.AddDays(1);

				if (IsBusinessDay(result))
					daysAdded++;
			}

			return result;
		}

		public bool IsBusinessDay(DateTime date)
		{
			return date.DayOfWeek != DayOfWeek.Saturday &&
					date.DayOfWeek != DayOfWeek.Sunday &&
					!_holidays.Contains(date);
		}
	}
}