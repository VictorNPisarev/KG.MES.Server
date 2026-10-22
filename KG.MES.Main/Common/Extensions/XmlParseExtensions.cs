namespace KG.MES.Main.Common.Extensions
{
	public static class XmlParseExtensions
	{
		/// <summary>
		/// Метод для парсинга дат в формате "2025-12-8"
		/// </summary>
		/// <param name="dateString"></param>
		/// <returns></returns>
		public static DateTime? ParseXmlDate(this string dateString)
		{
			if (string.IsNullOrEmpty(dateString))
				return null;
				
			try
			{
				// Парсим дату в формате "2025-12-8" (день и месяц без ведущих нулей)
				var parts = dateString.Split('-');
				if (parts.Length == 3)
				{
					int year = int.Parse(parts[0]);
					int month = int.Parse(parts[1]);
					int day = int.Parse(parts[2]);
					
					return new DateTime(year, month, day);
				}
				
				// Пробуем стандартный парсинг
				return DateTime.Parse(dateString);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Метод для сериализации дат обратно в строковый формат
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>
		public static string FormatDate(this DateTime? date)
		{
			if (!date.HasValue)
				return string.Empty;
				
			return $"{date.Value.Year}-{date.Value.Month}-{date.Value.Day}";
		}
	}
}