using KG.MES.Main.Interfaces;
using KG.MES.Main.Models;
using KG.MES.Main.Models.Enums;
using KG.MES.Main.Models.Xml;
using KG.MES.Main.Services;

namespace KG.MES.Main.Services
{
	public class DocumentItemFactory : IDocumentItemFactory
	{
		private readonly SortingIndicatorService _sortingService;
		private readonly IMaterialFactory _materialFactory;

		public DocumentItemFactory(
			SortingIndicatorService sortingService,
			IMaterialFactory materialFactory)
		{
			_sortingService = sortingService;
			_materialFactory = materialFactory;
		}

		public DocumentItem Create(XmlDocumentItem xmlData, DocumentData? documentData = null)
		{
			var item = new DocumentItem
			{
				documentData = documentData,
				ItemNumber = xmlData.ItemNumber,
				ItemId = xmlData.ItemId,
				ArticleNumber = xmlData.ArticleNumber,
				ShortDescription = xmlData.ShortDescription,
				Description = xmlData.Description,
				Quantity = xmlData.Quantity,
				Piece = xmlData.Piece,
				UnitPrice = xmlData.UnitPrice,
				Unit = xmlData.Unit,
				ModuleName = xmlData.ModuleName,
				ModuleSection = xmlData.ModuleSection,
				SortingIndicator = xmlData.SortingIndicator,
				ItemType = xmlData.ParseItemType(_sortingService),
				FormType = xmlData.GetFormType()
			};

			// MaterialList
			if (xmlData.xmlMaterialList != null)
				item.MaterialList = new MaterialList(xmlData.xmlMaterialList, item);

			// Accessories
			foreach (var accessory in xmlData.xmlAccessories)
				item.Accessories.Add(new Accessory(accessory));

			// Materials через фабрику
			LoadMaterials(item, xmlData);

			return item;
		}

		private void LoadMaterials(DocumentItem item, XmlDocumentItem xmlData)
		{
			item.Materials.Clear();

			if (xmlData.xmlMaterialList?.xmlWoodProfiles != null)
			{
				foreach (var woodProfile in xmlData.xmlMaterialList.xmlWoodProfiles)
				{
					LoadMaterials(item, woodProfile.xmlProfileDetailDatas);
				}
			}

			LoadMaterials(item, xmlData.xmlMaterialList?.xmlPaints);
			LoadMaterials(item, xmlData.xmlMaterialList?.xmlFittings);
			LoadMaterials(item, xmlData.xmlMaterialList?.xmlGlassProducts);
			LoadMaterials(item, xmlData.xmlAccessories);
			LoadMaterials(item, xmlData.xmlMaterialList?.xmlTechnicalArticles);
		}

		private void LoadMaterials<T>(DocumentItem item, IEnumerable<T>? items)
			where T : IXmlDataMaterial
		{
			if (items == null) return;

			foreach (var xmlMaterial in items)
			{
				var material = _materialFactory.CreateMaterial(xmlMaterial, item);
				if (material != null)
				{
					item.Materials.Add(material);
				}
			}
		}
	}
}