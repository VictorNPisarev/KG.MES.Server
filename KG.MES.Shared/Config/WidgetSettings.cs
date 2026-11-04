using KG.MES.Shared.Models.Enums;

namespace KG.MES.Shared.Models.Config
{
	public class WidgetSettings
	{
		public WidgetType WidgetId { get; set; }
		public string Title { get; set; } = string.Empty;
		public bool Visible { get; set; } = true;
		public int Order { get; set; }
		public int Column { get; set; } = 1; // 1 или 2 колонки
		public bool IsTabbed { get; set; }
		public string? CustomWidth { get; set; }
	}
}