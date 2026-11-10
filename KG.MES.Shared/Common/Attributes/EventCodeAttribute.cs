using System;

namespace KG.MES.Shared.Attributes;

/// <summary>
/// Задает код события для уведомлений (например, "NEW_ORDER")
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class EventCodeAttribute : Attribute
{
	public string Code { get; }

	public EventCodeAttribute(string code)
	{
		Code = code;
	}
}