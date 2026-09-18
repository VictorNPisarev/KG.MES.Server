using System.Xml.Serialization;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models.Enums;

namespace KG.MES.Main.Models.Xml
{
	public partial class XmlPaint
	{
		[XmlElement("paint_nr")]
		public string? PaintNumber { get; set; }
		
		[XmlElement("quantitysqm")]
		public decimal QuantitySqm { get; set; }
		
		[XmlElement("quantitym")]
		public decimal QuantityM { get; set; }
		
		[XmlElement("price")]
		public decimal Price { get; set; }
	}

	/// <summary>
	/// Реализация интерфейса + поля из вложенных блоков
	/// </summary>
	public partial class XmlPaint : IXmlDataMaterial
	{
		[XmlIgnore]
		public XmlBlockType XmlBlockType => XmlBlockType.XmlPaint;

		[XmlIgnore]
		public string? ArticleNumber => PaintNumber;

		[XmlIgnore]
		public string? Name => PaintNumber;

		public decimal Quantity => QuantitySqm > 0 ? QuantitySqm : QuantityM;

		public int PieceCount => 1;

		public string? Unit => QuantitySqm > 0 ? "м²" : "м";

		public decimal UnitPrice => Price;

		public void Validate() {}
	}
}