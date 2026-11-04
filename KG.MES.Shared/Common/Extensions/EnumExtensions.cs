using System.ComponentModel;
using System.Reflection;
using KG.MES.Shared.Common.Attributes;

namespace KG.MES.Shared.Common.Extensions
{
	public static class EnumExtensions
	{
		public static string GetDescription(this Enum value)
		{
			var field = value.GetType().GetField(value.ToString());
			var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
			return attribute?.Description ?? value.ToString();
		}

		public static string GetIcon(this Enum value)
		{
			var field = value.GetType().GetField(value.ToString());
			var attribute = field?.GetCustomAttribute<IconAttribute>();
			return attribute?.IconName ?? "bi bi-tag";
		}

		public static string GetColor(this Enum value)
		{
			var field = value.GetType().GetField(value.ToString());
			var attribute = field?.GetCustomAttribute<ColorAttribute>();
			return attribute?.ColorName ?? "secondary";
		}

	}
}