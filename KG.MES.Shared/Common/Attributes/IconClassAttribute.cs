[AttributeUsage(AttributeTargets.Field)]
public class IconClassAttribute : Attribute
{
	public string IconClass { get; }
	public string CssClass { get; }
	public string Title { get; }

	public IconClassAttribute(string iconClass, string cssClass, string title)
	{
		IconClass = iconClass;
		CssClass = cssClass;
		Title = title;
	}
}