using System.ComponentModel;
using KG.MES.Shared.Common.Attributes;

namespace KG.MES.Main.Models.Enums
{
	public enum DocumentItemType
	{
		[Description("Конструкция")]
		[Icon("bi-border-all")]
		Konstruktion,

		[Description("Артикул")]
		[Icon("bi-box")]
		Article,

		[Description("Технический артикул")]
		[Icon("bi-gear")]
		TechnikArticle,

		[Description("Щитовое изделие")]
		[Icon("bi-layers-fill")]
		PanelProducts,

		[Description("Стеклопакет")]
		[Icon("bi-aspect-ratio")]
		GlassProduct,

		[Description("Москитная сетка")]
		[Icon("bi-grid-3x3")]
		MosquitoNet,

		[Description("Обсада")]
		[Icon("bi-exclude")]
		Casing
	}
}