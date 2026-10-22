using System.Text.Json;

namespace KG.MES.Shared.Helpers;

public static class TableSettingsManager
{
	private static readonly Dictionary<string, TableSettings> _cache = new();

	public static List<ColumnSetting> GetSettings<TOrder>(string? json)
	{
		var tableKey = typeof(TOrder).Name;
		var defaults = ColumnHelper.GetDefaultSettings<TOrder>();

		if (!string.IsNullOrEmpty(json))
		{
			try
			{
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var saved = JsonSerializer.Deserialize<List<ColumnSetting>>(json, options);

				if (saved != null && saved.Count > 0)
				{
					// Дополняем дефолтными, если каких-то нет
					foreach (var def in defaults)
					{
						if (!saved.Any(s => s.PropertyName == def.PropertyName))
							saved.Add(def);
					}
					return saved;
				}
			}
			catch { }
		}

		return defaults;
	}

	public static List<ColumnSetting> GetDefaultSettings<TOrder>()
	{
		return ColumnHelper.GetDefaultSettings<TOrder>();
	}

	public static string Serialize(List<ColumnSetting> settings)
	{
		return JsonSerializer.Serialize(settings);
	}
}
