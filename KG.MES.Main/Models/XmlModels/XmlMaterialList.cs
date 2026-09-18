using System.Xml.Serialization;
using KG.MES.Main.Interfaces;

namespace KG.MES.Main.Models.Xml
{
	public partial class XmlMaterialList
	{
		[XmlElement("quantity_windows")]
		public int QuantityWindows { get; set; }
		
		[XmlElement("quantity_glasses")]
		public int QuantityGlasses { get; set; }
		
		[XmlArray("paints")]
		[XmlArrayItem("paint")]
		public List<XmlPaint> xmlPaints { get; set; } = new();

		[XmlArray("fittings")]
		[XmlArrayItem("fit")]
		public List<XmlFitting> xmlFittings { get; set; } = new();
		
		[XmlElement("glasprodukte")]
		public XmlGlassProductsList xmlGlassProductsList { get; set; } = new();

		[XmlArray("profiles")]
		[XmlArrayItem("prf_wood")]
		public List<XmlWoodProfile> xmlWoodProfiles { get; set; } = new();

		// Добавляем технические статьи
		[XmlArray("tech_article")]
		[XmlArrayItem("art_tech")]
		public List<XmlTechnicalArticle> xmlTechnicalArticles { get; set; } = new();

		[XmlArray("accessory_list")]
		[XmlArrayItem("accessory_item")]
		public List<XmlAccessory> xmlAccessories { get; set; } = new();
	}

	public partial class XmlMaterialList : IXmlDataModel
	{
		[XmlIgnore]
		public List<XmlGlassProduct> xmlGlassProducts => 
			xmlGlassProductsList.XmlGlassProducts;
		public void Validate()
		{}		
	}
}