using System.Xml.Serialization;
using KG.MES.Shared.Common.Extensions;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models.Enums;

namespace KG.MES.Main.Models.Xml
{
	public partial class XmlTechnicalArticle
	{
		[XmlElement("component")]
		public string? Component { get; set; }
		
		[XmlElement("frame_nr")]
		public int FrameNumber { get; set; }
		
		[XmlElement("base_article_number")]
		public string? BaseArticleNumber { get; set; }
		
		[XmlElement("description")]
		public string? Description { get; set; }
		
		[XmlElement("deliverykind")]
		public string? DeliveryKind { get; set; }
		
		[XmlElement("cuttings_percentage")]
		public decimal CuttingsPercentage { get; set; }
		
		[XmlElement("price")]
		public decimal Price { get; set; }
		
		[XmlElement("rodlength")]
		public decimal RodLength { get; set; }
		
		[XmlElement("item_measurement")]
		public XmlItemMeasurement? ItemMeasurement { get; set; }
	}

	/// <summary>
	/// Поля из вложенных блоков
	/// </summary>
	public partial class XmlTechnicalArticle : IXmlDataMaterial
	{
		[XmlIgnore]
		public string? Unit => ItemMeasurement?.Measure?.Unit;

		[XmlIgnore]
		public XmlBlockType XmlBlockType => XmlBlockType.XmlTechArticle;

		[XmlIgnore]
		public string? ArticleNumber => BaseArticleNumber;

		[XmlIgnore]
		public string? Name => Description;

		[XmlIgnore]
		public decimal Quantity => (decimal)(ItemMeasurement?.Quantity ?? 0);

		[XmlIgnore]
		public decimal UnitPrice => Price;

		[XmlIgnore]
		public int PieceCount => 1;

		public Dictionary<string, object> GetAttributes()
		{
			var attributes = new Dictionary<string, object>();
			
			attributes.AddIfNotEmpty("Component", Component);
			attributes.AddIfHasValue("FrameNumber", FrameNumber);
			attributes.AddIfHasValue("RodLength", RodLength);
			
			return attributes;
		}

		public void Validate()
		{
			throw new NotImplementedException();
		}
	}
}