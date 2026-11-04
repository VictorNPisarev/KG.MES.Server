using KG.MES.Shared.Models.Enums;

namespace KG.MES.Shared.Models.Config
{
	public class DashboardSettings
	{
		public List<WidgetSettings> Widgets { get; set; } = new();
	}
}