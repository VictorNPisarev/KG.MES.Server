using System.ComponentModel;
using System.Reflection;
using KG.MES.Shared.Attributes;
using KG.MES.Shared.Model.Enums;

namespace KG.MES.Server.Extensions;

public static class NotificationEventExtensions
{
	/// <summary>
	/// Получить код события (из атрибута [EventCode])
	/// </summary>
	public static string ToCode(this NotificationEventType type)
	{
		var field = type.GetType().GetField(type.ToString());

		var attribute = field?.GetCustomAttribute<EventCodeAttribute>();

		if (attribute == null)
			throw new InvalidOperationException(
				$"EventCode attribute not found for {type}");

		return attribute?.Code ?? type.ToString();
	}

	/// <summary>
	/// Получить описание (из атрибута [Description])
	/// </summary>

	public static string ToDescription(this NotificationEventType type)
	{
		var field = type.GetType().GetField(type.ToString());
		var attribute = field?.GetCustomAttribute<DescriptionAttribute>();

		return attribute?.Description ?? type.ToString();
	}
}