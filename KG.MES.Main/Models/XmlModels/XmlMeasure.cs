using System.Xml.Serialization;
using KG.MES.Main.Interfaces;

namespace KG.MES.Main.Models.Xml
{
	/// <summary>
	/// <measure> в <art_tech> - технические артикулы
	/// </summary>
	public partial class XmlMeasure : IXmlDataModel
	{
		[XmlElement("width_length")]
		public double WidthLength { get; set; }
		
		[XmlElement("height")]
		public double Height { get; set; }
		
		[XmlElement("depth")]
		public double Depth { get; set; }
		
		[XmlElement("pieces")]
		public int Pieces { get; set; }
		
		[XmlElement("unit")]
		public string? Unit { get; set; }

		public void Validate () {}
	}

	/// <summary>
	/// <measure> в <profiledetaildata> - дерево
	/// </summary>
	public partial class XmlMeasure : IXmlDataModel
	{
		[XmlElement("cuttinglength")]
		public decimal? CuttingLength { get; set; }
		
		[XmlElement("square_nr")]
		public string? SquareNr { get; set; }
		
		[XmlElement("square_timberkind")]
		public string? SquareTimberKind { get; set; }

		[XmlElement("squarewidth")]
		public decimal? SquareWidth { get; set; }

		[XmlElement("squarethickness")]
		public decimal? SquareThickness { get; set; }

		[XmlElement("squarelength")]
		public decimal? SquareLength { get; set; }

		[XmlElement("squareprice")]
		public decimal? SquarePrice { get; set; }

		[XmlElement("order_nr")]
		public string? OrderNr { get; set; }
		
		[XmlElement("squareidentifierpieceprice")]
		public string? SquareIdentifierPiecePrice { get; set; }

		[XmlElement("piece")]
		public int Piece { get; set; }
		
		[XmlElement("finishedlength")]
		public decimal? FinishedLength { get; set; }
	}

	/// <summary>
	/// <measure> в <fittings> - фурнитура
	/// </summary>
	public partial class XmlMeasure : IXmlDataModel
	{
		[XmlElement("length1")]
		public decimal? Length1 { get; set; }

		[XmlElement("length2")]
		public decimal? Length2 { get; set; }

		//[XmlElement("piece")]
		//public int Piece { get; set; } // Есть в дереве

		[XmlElement("price")]
		public decimal? Price { get; set; }

		[XmlElement("grossprice")]
		public decimal? GrossPrice { get; set; }

		[XmlElement("discountgroup")]
		public string? DiscountGroup { get; set; }

		[XmlElement("discountpercent")]
		public decimal? DiscountPercent { get; set; }
	}

}