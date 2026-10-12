namespace KG.MES.Shared.Common.Extensions
{
	/// <summary>
	/// Расширение для словаря, чтобы при добавлении проверять на null и пустые значения, и не добавлять соответствующие элементы
	/// </summary>
	public static class DictionaryExtensions
	{
		// Для ссылочных типов (string, object и т.д.)
		public static void AddIfNotNull<T>(this Dictionary<string, object> dict, string key, T? value) where T : class
		{
			if (value != null)
				dict[key] = value;
		}

		// Для nullable значимых типов (int?, decimal?, DateTime? и т.д.)
		public static void AddIfHasValue(this Dictionary<string, object> dict, string key, int? value)
		{
			if (value.HasValue)
				dict[key] = value.Value;
		}

		public static void AddIfHasValue(this Dictionary<string, object> dict, string key, decimal? value)
		{
			if (value.HasValue)
				dict[key] = value.Value;
		}

		public static void AddIfHasValue(this Dictionary<string, object> dict, string key, double? value)
		{
			if (value.HasValue)
				dict[key] = value.Value;
		}


		// Для строк с проверкой на пустоту
		public static void AddIfNotEmpty(this Dictionary<string, object> dict, string key, string? value)
		{
			if (!string.IsNullOrWhiteSpace(value))
				dict[key] = value;
		}
	}
}