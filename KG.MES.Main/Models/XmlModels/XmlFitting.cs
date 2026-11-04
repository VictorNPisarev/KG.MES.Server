using System.Xml.Serialization;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models.Enums;

namespace KG.MES.Main.Models.Xml
{
	public partial class XmlFitting
	{
		[XmlElement("fitting_nr")]
		public string? FittingNumber { get; set; }
		
		[XmlElement("fitting_desc")]
		public string? FittingDescription { get; set; }
		
		[XmlElement("item_measurement")]
		public XmlItemMeasurement? ItemMeasurement { get; set; }
	}

	/// <summary>
	/// Реализация интерфейса + поля из вложенных блоков
	/// </summary>
	public partial class XmlFitting : IXmlDataMaterial
	{
		[XmlIgnore]
		public int PieceCount => ItemMeasurement?.Measure?.Piece ?? 0;

		[XmlIgnore]
		public decimal? Price => ItemMeasurement?.Measure?.Price;

		[XmlIgnore]
		public XmlBlockType XmlBlockType => XmlBlockType.XmlFitting;

		[XmlIgnore]
		public string? ArticleNumber => FittingNumber;

		[XmlIgnore]
		public string? Name => FittingDescription;

		[XmlIgnore]
		public decimal Quantity => PieceCount;

		[XmlIgnore]
		public string? Unit => null;

		[XmlIgnore]
		public decimal UnitPrice => Price ?? 0;

		public void Validate() {}
	}

}
