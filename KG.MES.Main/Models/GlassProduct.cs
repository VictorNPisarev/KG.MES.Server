using System.Xml.Serialization;
using KG.MES.Main.Models.Enums;
using KG.MES.Main.Models.Xml;

namespace KG.MES.Main.Models
{
	public class GlassProduct
	{
		/// <summary>
		/// Профильная система
		/// </summary>
		public string? ProfileGroup { get; set; }
		
		/// <summary>
		/// Номер проема
		/// </summary>
		public int ElementNumber { get; set; }
		
		/// <summary>
		/// Ширина
		/// </summary>
		public int GlassWidth { get; set; }
		
		/// <summary>
		/// Высота
		/// </summary>
		public int GlassHeight { get; set; }
		
		/// <summary>
		/// Площадь пакета
		/// </summary>
		public decimal GlassArea { get; set; }
		
		/// <summary>
		/// Формула пакета
		/// </summary>
		public string? GlassProductType { get; set; } = string.Empty;

		/// <summary>
		/// Форма СП
		/// </summary>
		public FormType FormType { get; set; } = FormType.Rectangular;

		public GlassProduct () {}

		public GlassProduct (XmlGlassProduct xmlGlassProduct)
		{
			ProfileGroup = xmlGlassProduct.ProfileGroup;
			ElementNumber = xmlGlassProduct.ElementNumber;
			GlassWidth = xmlGlassProduct.GlassWidth;
			GlassHeight = xmlGlassProduct.GlassHeight;
			GlassArea = xmlGlassProduct.GlassArea;
			GlassProductType = xmlGlassProduct.GlassProduct;
			FormType = xmlGlassProduct.ParseFormType();
		}

		public void Validate ()
		{
			if (string.IsNullOrWhiteSpace(GlassProductType))
				throw new ArgumentException("GlassProductType is required");

		}
	}
}
