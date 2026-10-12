// Enums/IndicatorCategoryType.cs
using System.ComponentModel;
using KG.MES.Shared.Common.Attributes;

namespace KG.MES.Main.Models.Enums
{
	public enum IndicatorCategoryType
	{
		[Description("Щитовые изделия")]
		[Icon("bi bi-grid-3x3-gap-fill")]
		[Color("success")]
		PanelProducts,
		
		[Description("Стеклопакеты")]
		[Icon("bi bi-window")]
		[Color("info")]
		GlassProducts,
		
		[Description("Фурнитура")]
		[Icon("bi bi-gear")]
		[Color("warning")]
		Hardware,
		
		[Description("Материалы для отделки")]
		[Icon("bi bi-palette")]
		[Color("secondary")]
		FinishingMaterials,
		
		[Description("Алюминиевые профили")]
		[Icon("bi bi-box-seam")]
		[Color("primary")]
		AluminumProfiles,
		
		[Description("Упаковка")]
		[Icon("bi bi-box")]
		[Color("secondary")]
		Packaging,
		
		[Description("Москитные сетки")]
		[Icon("bi bi-fence")]
		[Color("info")]
		MosquitoNets,
		
		[Description("Вентиляция")]
		[Icon("bi bi-fan")]
		[Color("primary")]
		Ventilation,
		
		[Description("Подоконники")]
		[Icon("bi bi-window-sill")]
		[Color("success")]
		WindowSills,
		
		[Description("Откосы")]
		[Icon("bi bi-layout-split")]
		[Color("success")]
		Slopes,
		
		[Description("Наличники")]
		[Icon("bi bi-border-all")]
		[Color("success")]
		Casings
	}
}