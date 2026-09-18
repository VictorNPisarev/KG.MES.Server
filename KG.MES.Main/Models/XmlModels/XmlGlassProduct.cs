using System.Xml.Serialization;
using KG.MES.Main.Common.Extensions;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models.Enums;
using KG.MES.Shared.Common.Extensions;

namespace KG.MES.Main.Models.Xml
{
	public partial class XmlGlassProduct
	{
		[XmlElement("profilegroup")]
		public string? ProfileGroup { get; set; }
		
		[XmlElement("element_nr")]
		public int ElementNumber { get; set; }
		
		[XmlElement("glasswidth")]
		public int GlassWidth { get; set; }
		
		[XmlElement("glassheight")]
		public int GlassHeight { get; set; }
		
		[XmlElement("glassarea")]
		public decimal GlassArea { get; set; }
		
		[XmlElement("glasprodukt")]
		public string? GlassProduct { get; set; }

		[XmlElement("glasspiece")]
		public int GlassPiece { get; set; }

		[XmlElement("bestelltext")]
		public string? BestellText { get; set; }
		
		[XmlElement("glassunitprice")]
		public decimal GlassUnitPrice { get; set; }

		[XmlElement("piecekind")]
		public string? PieceKind { get; set; }
	}
		
	/// <summary>
	/// Реализация интерфейса + поля из вложенных блоков
	/// </summary>
	public partial class XmlGlassProduct : IXmlDataMaterial
	{
		[XmlIgnore]
		public XmlBlockType XmlBlockType => XmlBlockType.XmlGlassProduct;

		[XmlIgnore]
		public string? ArticleNumber => GlassProduct;

		[XmlIgnore]
		public string? Name => BestellText;

		[XmlIgnore]
		public decimal Quantity => GlassArea;

		[XmlIgnore]
		public int PieceCount => GlassPiece;

		[XmlIgnore]
		public string? Unit => null;

		[XmlIgnore]
		public decimal UnitPrice => GlassUnitPrice;

		public Dictionary<string, object> GetAttributes()
		{
			var attributes = new Dictionary<string, object>();

			attributes.AddIfHasValue("GlassWidth", GlassWidth);
			attributes.AddIfHasValue("GlassHeight", GlassHeight);
			attributes.AddIfHasValue("GlassArea", GlassArea);
			attributes.AddIfNotEmpty("ProfileGroup", ProfileGroup);
			attributes.AddIfHasValue("GlassPiece", GlassPiece);

			return attributes;
		}
		public void Validate() {}
	}

	static class XmlGlassProductExtensions
	{
		public static FormType ParseFormType(this XmlGlassProduct xmlGlassProduct)
		{
			var pieceKind = xmlGlassProduct.PieceKind?.ToLowerInvariant() ?? "";
			
			// Приоритет: если есть 'r' — это арка (независимо от наличия 's')
			if (pieceKind.Contains('r'))
				return FormType.Arch;
			
			// Если нет 'r', но есть 's' — косоугольная
			if (pieceKind.Contains('s'))
				return FormType.Sloped;
			
			// Во всех остальных случаях — прямоугольная
			return FormType.Rectangular;
		}
	}
}
