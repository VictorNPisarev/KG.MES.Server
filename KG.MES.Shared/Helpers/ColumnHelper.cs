using System.Reflection;
using KG.MES.Shared.Attributes;
using KG.MES.Shared.Models.Enums;

namespace KG.MES.Shared.Helpers;

public static class ColumnHelper
{
	public static List<ColumnInfo> GetColumns<T>()
	{
		return typeof(T).GetProperties()
			.Select(p => new
			{
				Property = p,
				Attr = p.GetCustomAttribute<ColumnAttribute>()
			})
			.Where(x => x.Attr != null)
			.OrderBy(x => x.Attr!.Order)
			.Select(x => new ColumnInfo
			{
				PropertyName = x.Property.Name,
				Title = x.Attr!.Title,
				DisplayFormat = x.Attr.DisplayFormat,
				IsBadge = x.Attr.IsBadge,
				BadgeGroup = x.Attr.BadgeGroup,
				CommentField = x.Attr.CommentField,
				DisplayGroup = x.Attr.DisplayGroup,
				IconConditions = x.Attr.IconConditions,
				ShowTotal = x.Attr.ShowTotal,
				Sortable = x.Attr.Sortable,
				Filterable = x.Attr.Filterable
			})
			.ToList();
	}

	public static string? GetFormattedValue(object obj, ColumnInfo column)
	{
		var property = obj.GetType().GetProperty(column.PropertyName);
		var value = property?.GetValue(obj);

		if (value == null) return null;

		if (!string.IsNullOrEmpty(column.DisplayFormat))
		{
			if (value is DateTime dt)
				return dt.ToString(column.DisplayFormat);
			if (value is double d)
				return d.ToString(column.DisplayFormat);
			if (value is decimal m)
				return m.ToString(column.DisplayFormat);
		}

		if (value is string s && !string.IsNullOrEmpty(column.DisplayGroup))
			return BadgeHelper.GetDisplayValue(s, column.DisplayGroup);

		return value.ToString();
	}

	public static List<ColumnSetting> GetDefaultSettings<T>()
	{
		var settings = typeof(T).GetProperties()
			.Select(p => new
			{
				Property = p,
				Attr = p.GetCustomAttribute<ColumnAttribute>()
			})
			.Where(x => x.Attr != null)
			.Select(x => new ColumnSetting
			{
				PropertyName = x.Property.Name,
				Visible = x.Attr!.Visible,
				Order = x.Attr.Order,
				Width = 0
			})
			.ToList();

		// Перенумеровываем подряд, начиная с 0
		var ordered = settings.OrderBy(s => s.Order).ToList();
		for (int i = 0; i < ordered.Count; i++)
			ordered[i].Order = i;

		return settings;
	}

	public static List<IconInfo> GetIcons(object obj, string[]? iconConditions)
	{
		var result = new List<IconInfo>();
		if (iconConditions == null) return result;

		foreach (var condition in iconConditions)
		{
			var parts = condition.Split(':');
			if (parts.Length != 2) continue;

			var propertyName = parts[0].Trim();
			var iconName = parts[1].Trim();

			var prop = obj.GetType().GetProperty(propertyName);
			if (prop == null) continue;

			var value = prop.GetValue(obj);
			if (value is bool b && b && Enum.TryParse<OrderIcon>(iconName, out var icon))
			{
				var attr = icon.GetType().GetField(icon.ToString())!
					.GetCustomAttribute<IconClassAttribute>();

				if (attr != null)
				{
					result.Add(new IconInfo
					{
						Class = attr.IconClass,
						Css = attr.CssClass,
						Title = attr.Title
					});
				}
			}
		}

		return result;
	}

	public static List<IconInfo> GetIcons(object obj, Type type, string? propertyNameFilter = null)
	{
		var result = new List<IconInfo>();

		foreach (var prop in type.GetProperties())
		{
			if (propertyNameFilter != null && prop.Name != propertyNameFilter) continue;

			var attr = prop.GetCustomAttribute<ColumnAttribute>();
			
			if (attr?.IconConditions == null) continue;
			
			result.AddRange(GetIcons(obj, attr.IconConditions));
		}

		return result;
	}
}
