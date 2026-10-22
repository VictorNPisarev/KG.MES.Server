// Common/Attributes/IconAttribute.cs
namespace KG.MES.Shared.Common.Attributes
{
	[AttributeUsage(AttributeTargets.Field)]
	public class IconAttribute : Attribute
	{
		public string IconName { get; }
		public IconAttribute(string iconName) => IconName = iconName;
	}
}