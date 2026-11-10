[AttributeUsage(AttributeTargets.Field)]
public class IconClassAttribute : Attribute
{
	public string IconClass { get; }
	public string CssClass { get; }
	public string Title { get; }
	public bool Forvard { get; set; }

	public IconClassAttribute(string iconClass, string cssClass, string title, bool forvard = true)
	{
		IconClass = iconClass;
		CssClass = cssClass;
		Title = title;
		Forvard = forvard;
	}
}