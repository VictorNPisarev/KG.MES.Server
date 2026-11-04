using System.Text.Json;
using System.Text.Json.Serialization;

namespace KG.MES.Shared.Helpers;

public static class BadgeHelper
{
	private static Dictionary<string, Dictionary<string, string>> _badgeStyles = [];
	private static Dictionary<string, string> _defaultStyles = [];
	private static Dictionary<string, string> _statusDisplayNames = [];
	private static Dictionary<string, Dictionary<string, string>> _displayValues = [];
	public static void LoadConfig(string baseConfigPath, string? appConfigPath = null)
	{
		if (File.Exists(baseConfigPath))
			MergeConfig(File.ReadAllText(baseConfigPath));

		if (!string.IsNullOrEmpty(appConfigPath) && File.Exists(appConfigPath))
			MergeConfig(File.ReadAllText(appConfigPath));
	}

	private static void MergeConfig(string json)
	{
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var config = JsonSerializer.Deserialize<BadgeConfig>(json, options);

		if (config?.Groups != null)
		{
			foreach (var (groupName, styles) in config.Groups)
			{
				if (!_badgeStyles.ContainsKey(groupName))
					_badgeStyles[groupName] = [];

				foreach (var kvp in styles)
					_badgeStyles[groupName][kvp.Key.ToLower()] = kvp.Value;
			}
		}

		if (config?.Defaults != null)
		{
			foreach (var kvp in config.Defaults)
				_defaultStyles[kvp.Key] = kvp.Value;
		}

		if (config?.Displays != null)
		{
			foreach (var (group, values) in config.Displays)
			{
				if (!_displayValues.ContainsKey(group))
					_displayValues[group] = new();
				foreach (var kvp in values)
					_displayValues[group][kvp.Key.ToLower()] = kvp.Value;
			}
		}
	}

	public static void RegisterStatusDisplayName(string code, string displayName)
	{
		_statusDisplayNames[code] = displayName;
	}

	public static string GetStatusDisplayName(string? code)
	{
		if (code == null) return "—";
		return _statusDisplayNames.TryGetValue(code, out var name) ? name : code;
	}

	public static string GetBadgeClass(string? value, string? group = null)
	{
		if (string.IsNullOrEmpty(value))
			return _defaultStyles.GetValueOrDefault(group ?? "order_status", "bg-light text-dark");

		// Ищем в указанной группе
		if (group != null && _badgeStyles.TryGetValue(group, out var groupStyles))
		{
			if (groupStyles.TryGetValue(value.ToLower(), out var cls))
				return cls;
		}

		// Ищем по всем группам
		foreach (var g in _badgeStyles.Values)
		{
			if (g.TryGetValue(value.ToLower(), out var cls))
				return cls;
		}

		return _defaultStyles.GetValueOrDefault(group ?? "order_status", "bg-light text-dark");
	}

	public static string GetFormattedBadgeValue(object obj, ColumnInfo column)
	{
		var propertyName = column.BadgeProperty ?? column.PropertyName;
		var property = obj.GetType().GetProperty(propertyName);
		var value = property?.GetValue(obj);

		return value switch
		{
			bool b => b ? "Да" : "Нет",
			string s => GetDisplayValue(s, column.DisplayGroup),
			null => "—",
			_ => value.ToString() ?? "—"
		};
	}

	public static string GetDisplayValue(string? value, string? group = null)
	{
		if (string.IsNullOrEmpty(value)) return "—";

		if (group != null && _displayValues.TryGetValue(group, out var groupValues))
		{
			if (groupValues.TryGetValue(value.ToLower(), out var display))
				return display;
		}

		// Ищем по всем группам
		foreach (var g in _displayValues.Values)
		{
			if (g.TryGetValue(value.ToLower(), out var display))
				return display;
		}

		return value;
	}

	private class BadgeConfig
	{
		[JsonPropertyName("groups")]
		public Dictionary<string, Dictionary<string, string>> Groups { get; set; } = [];

		[JsonPropertyName("defaults")]
		public Dictionary<string, string> Defaults { get; set; } = [];

		[JsonPropertyName("displays")]
		public Dictionary<string, Dictionary<string, string>> Displays { get; set; } = [];
	}
}
