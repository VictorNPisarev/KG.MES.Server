using System.Reflection.Metadata.Ecma335;
using System.Xml.Serialization;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models.Enums;
using KG.MES.Main.Services;

namespace KG.MES.Main.Models.Xml
{
	/// <summary>
	/// Модель XML файла
	/// </summary>
	public class XmlDocumentItem : IXmlDataModel
	{
		[XmlElement("item_type")]
		public string ItemTypeXml { get; set; } = string.Empty;

		[XmlElement("item_number")]
		public int ItemNumber { get; set; }
		
		[XmlElement("item_id")]
		public string? ItemId { get; set; }
		
		// Артикулы
		[XmlElement("article_purchase")]
		public bool ArticlePurchase { get; set; }
		
		[XmlElement("article_number_internal")]
		public string? ArticleNumberInternal { get; set; }
		
		[XmlElement("item_number_internal")]
		public string? ItemNumberInternal { get; set; }
		
		[XmlElement("item_number_architect")]
		public string? ItemNumberArchitect { get; set; }
		
		[XmlElement("article_number")]
		public string? ArticleNumber { get; set; }
		
		// Описания
		[XmlElement("item_short_description")]
		public string? ShortDescription { get; set; }
		
		[XmlElement("bestellcode")]
		public string? BestellCode { get; set; }
		
		[XmlElement("installation_kind")]
		public string? InstallationKind { get; set; }
		
		[XmlElement("cuttings_percentage")]
		public decimal CuttingsPercentage { get; set; }
		
		[XmlElement("item_text")]
		public string? Description { get; set; }
		
		[XmlElement("item_text_work_order")]
		public string? WorkOrderText { get; set; }
		
		[XmlElement("sorting_indicator")]
		public string? SortingIndicator { get; set; }
		
		// Количество и цены
		[XmlElement("item_quantity")]
		public decimal Quantity { get; set; }
		
		[XmlElement("item_prime_cost")]
		public decimal PrimeCost { get; set; }
		
		[XmlElement("urekpreis")]
		public decimal UnitPrice { get; set; }
		
		[XmlElement("piece")]
		public int Piece { get; set; }
		
		[XmlElement("unit")]
		public string? Unit { get; set; }
		
		[XmlElement("quantity_calculation_type")]
		public string? QuantityCalculationType { get; set; }
		
		// Модули и техданные
		[XmlElement("modulname")]
		public string? ModuleName { get; set; }
		
		[XmlElement("modulsektion")]
		public string? ModuleSection { get; set; }
		
		[XmlElement("technikdaten")]
		public string? TechnicalData { get; set; }
		
		// Вложенные данные
		[XmlElement("matlist")]
		public XmlMaterialList? xmlMaterialList { get; set; }
		
		[XmlArray("accessory_list")]
		[XmlArrayItem("accessory_item")]
		public List<XmlAccessory> xmlAccessories { get; set; } = new();

		public XmlDocumentItem()
		{
			xmlMaterialList = new XmlMaterialList();
		}

		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(ItemTypeXml))
				throw new ArgumentException("Item type is required");
		}
	}
	
	/// <summary>
	/// Методы расширения
	/// </summary>
	static class XmlDocumentItemExtensions
	{
		public static DocumentItemType ParseItemType(this XmlDocumentItem xmlDocumentItem,
			SortingIndicatorService sortingService) => 
			xmlDocumentItem.ItemTypeXml.ToLowerInvariant() switch
			{
				"konstruktion" => xmlDocumentItem.ParseKonstructionType(),
				"artikel" => xmlDocumentItem.ParseArticleType(sortingService),
				"technikartikel" => DocumentItemType.TechnikArticle,
				_ => throw new ArgumentException($"Неизвестный тип элемента: {xmlDocumentItem?.ItemTypeXml}")
			};
		
		private static DocumentItemType ParseKonstructionType(this XmlDocumentItem xmlDocumentItem)
		{
			//Стеклопакет содержит в артикуле "SP" (без учета регистра)
			if (!string.IsNullOrEmpty(xmlDocumentItem.ArticleNumber) && xmlDocumentItem.ArticleNumber.Contains("SP", StringComparison.OrdinalIgnoreCase))
			{
				return DocumentItemType.GlassProduct;
			}

			//Москитная сетка содержит в артикуле "MS" (без учета регистра)
			if (!string.IsNullOrEmpty(xmlDocumentItem.ArticleNumber) && xmlDocumentItem.ArticleNumber.Contains("MS", StringComparison.OrdinalIgnoreCase))
			{
				return DocumentItemType.MosquitoNet;
			}

			//Обсада содержит в артикуле "OB" или "ob" (без учета регистра)
			if (!string.IsNullOrEmpty(xmlDocumentItem.ArticleNumber) && xmlDocumentItem.ArticleNumber.Contains("OB", StringComparison.OrdinalIgnoreCase))
			{
				return DocumentItemType.Casing;
			}

			return DocumentItemType.Konstruktion;
		}
		private static DocumentItemType ParseArticleType(this XmlDocumentItem xmlDocumentItem, SortingIndicatorService sortingService)
		{
			if(sortingService.IsInCategory(xmlDocumentItem.SortingIndicator ?? "", IndicatorCategoryType.PanelProducts))
			{
				return DocumentItemType.PanelProducts;
			}
			else
			{
				return DocumentItemType.Article;
			}
		}

		public static FormType GetFormType(this XmlDocumentItem xmlDocumentItem)
		{
			if (xmlDocumentItem.xmlMaterialList?.xmlGlassProducts == null || 
				!xmlDocumentItem.xmlMaterialList.xmlGlassProducts.Any())
				return FormType.Undefined;
			
			foreach (var glass in xmlDocumentItem.xmlMaterialList.xmlGlassProducts)
			{
				var form = glass.ParseFormType();
				if (form == FormType.Arch)
					return FormType.Arch;
			}
			
			// Если нет арок, проверяем наличие косоугольных
			if (xmlDocumentItem.xmlMaterialList.xmlGlassProducts.Any(g => g.ParseFormType() == FormType.Sloped))
				return FormType.Sloped;
			
			return FormType.Rectangular;
		}

		public static bool IsInCategory(this XmlDocumentItem xmlDocumentItem, IndicatorCategoryType category, SortingIndicatorService sortingService)
		{
			return sortingService.IsInCategory(xmlDocumentItem.SortingIndicator ?? "", category);
		}

	}
}
