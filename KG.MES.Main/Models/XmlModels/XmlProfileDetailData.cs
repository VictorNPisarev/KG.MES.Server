using System.Xml.Serialization;
using KG.MES.Shared.Common.Extensions;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models.Enums;

namespace KG.MES.Main.Models.Xml
{
	public partial class XmlProfileDetailData
	{
		[XmlElement("timberkind")]
		public string? TimberKind { get; set; }
		
		[XmlElement("timberkind_desc")]
		public string? TimberKindDescription { get; set; }
		
		[XmlElement("finishedwidth")]
		public decimal FinishedWidth { get; set; }
		
		[XmlElement("cuttingwidth")]
		public decimal CuttingWidth { get; set; }
		
		[XmlElement("planewidth")]
		public decimal PlaneWidth { get; set; }
		
		[XmlElement("finishedthickness")]
		public decimal FinishedThickness { get; set; }
		
		[XmlElement("cuttungthickness")]
		public decimal CuttingThickness { get; set; }
		
		[XmlElement("planethickness")]
		public decimal PlaneThickness { get; set; }
		
		[XmlElement("cuttings_percentage")]
		public decimal CuttingsPercentage { get; set; }
		
		[XmlElement("profile_unit")]
		public string? ProfileUnit { get; set; }
		
		[XmlElement("laengenliste")]
		public string? LengthList { get; set; }
		
		[XmlElement("item_measurement")]
		public XmlItemMeasurement? ItemMeasurement { get; set; }
	}

	/// <summary>
	/// Поля из вложенных блоков
	/// </summary>
	public partial class XmlProfileDetailData : IXmlDataMaterial
	{
		[XmlIgnore]
		public decimal CuttingLength 
		{ 
			get { return ItemMeasurement?.Measure?.CuttingLength ?? 0m; } 
		}

		[XmlIgnore]
		public string SquareNr
		{ 
			get { return ItemMeasurement?.Measure?.SquareNr ?? string.Empty; } 
		}
		
		[XmlIgnore]
		public string SquareTimberKind
		{ 
			get { return ItemMeasurement?.Measure?.SquareTimberKind ?? string.Empty; } 
		}

		[XmlIgnore]
		public decimal? SquareWidth
		{ 
			get { return ItemMeasurement?.Measure?.SquareWidth ?? 0m; } 
		}

		[XmlIgnore]
		public decimal? SquareThickness
		{ 
			get { return ItemMeasurement?.Measure?.SquareThickness ?? 0m; } 
		}

		[XmlIgnore]
		public decimal? SquareLength
		{ 
			get { return ItemMeasurement?.Measure?.SquareLength ?? 0m; } 
		}

		[XmlIgnore]
		public decimal? SquarePrice
		{ 
			get { return ItemMeasurement?.Measure?.SquarePrice ?? 0m; } 
		}

		[XmlIgnore]
		public string? OrderNr
		{ 
			get { return ItemMeasurement?.Measure?.OrderNr ?? string.Empty; } 
		}
		
		[XmlIgnore]
		public string? SquareIdentifierPiecePrice
		{ 
			get { return ItemMeasurement?.Measure?.SquareIdentifierPiecePrice ?? string.Empty; } 
		}

		[XmlIgnore]
		public int Piece
		{ 
			get { return ItemMeasurement?.Measure?.Piece ?? 0; } 
		}
		
		[XmlIgnore]
		public decimal? FinishedLength
		{ 
			get { return ItemMeasurement?.Measure?.FinishedLength ?? 0m; } 
		}

		[XmlIgnore]
		public XmlBlockType XmlBlockType => XmlBlockType.XmlWoodProfile;

		[XmlIgnore]
		public string? ArticleNumber => TimberKind;

		[XmlIgnore]
		public string? Name => SquareNr;

		[XmlIgnore]
		public decimal Quantity => CuttingLength / 1000; // переводим мм в метры

		[XmlIgnore]
		public int PieceCount => Piece;

		[XmlIgnore]
		public string? Unit => ProfileUnit;

		[XmlIgnore]
		public decimal UnitPrice => SquarePrice ?? 0;

		public Dictionary<string, object> GetAttributes()
		{
			var attributes = new Dictionary<string, object>();

			attributes.AddIfNotNull("Component", TimberKind);
			attributes.AddIfNotNull("ProfileDescription", TimberKindDescription);
			attributes.AddIfHasValue("CuttingLength", CuttingLength);
			attributes.AddIfHasValue("SquareWidth", SquareWidth);
			attributes.AddIfHasValue("SquareThickness", SquareThickness);
			attributes.AddIfNotEmpty("SquareTimberKind", SquareTimberKind);

			return attributes;
		}


		public void Validate()
		{
			throw new NotImplementedException();
		}
	}
}