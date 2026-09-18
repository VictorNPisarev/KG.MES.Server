using System.Xml.Serialization;
using System.Linq;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models.Xml;
using System.Text.RegularExpressions;
using KG.MES.Main.Services;
using KG.MES.Main.Models.Enums;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Text.Json.Serialization;

namespace KG.MES.Main.Models
{
	/// <summary>
	/// Модель позиции заказа
	/// </summary>
	public partial class DocumentItem
	{
		/// <summary>
		/// Идентификатор
		/// </summary>
		public Guid Id { get; set; } = new();

		///<summary>
		///Тип позиции
		///</summary>
		public DocumentItemType ItemType { get; set; }
		
		///<summary>
		///Номер позиции
		///</summary>
		public int ItemNumber { get; set; }
		
		///<summary>
		///Id позиции
		///</summary>
		public string? ItemId { get; set; }
		
		///<summary>
		///Артикул позиции
		///</summary>
		public string? ArticleNumber { get; set; }
		
		///<summary>
		///Короткое описание позиции
		///</summary>
		public string? ShortDescription { get; set; }
		
		///<summary>
		///Описание позиции
		///</summary>
		public string? Description { get; set; }
		
		///<summary>
		///Количество в единицах Unit
		///</summary>
		public decimal Quantity { get; set; }

		///<summary>
		///Количество экземпляров позиции
		///</summary>
		public decimal Piece { get; set; }
		
		///<summary>
		///Цена позиции
		///</summary>
		public decimal UnitPrice { get; set; }
		
		///<summary>
		///Ед.измерения
		///</summary>
		public string? Unit { get; set; }

		/// <summary>
		/// Индикатор сортировки
		/// </summary>
		public string? SortingIndicator { get; set; }

		/// <summary>
		/// Форма окна (приоритет: арка > косое > прямоугольное)
		/// </summary>
		public FormType FormType { get; set; }

		public string? ModuleName { get; set; }
		
		public string? ModuleSection { get; set; }

		
		// Добавляем поддержку вложенных данных
		public MaterialList? MaterialList { get; set; } = new();
		
		public List<Accessory> Accessories { get; set; } = new();

		// Один список для всех материалов!
		public List<Material> Materials { get; set; } = new();


		/// <summary>
		/// Cсылка на родительский документ
		/// </summary>
		[JsonIgnore, XmlIgnore]
		public DocumentData? documentData { get; set; }
		
		// Конструктор для инициализации MaterialList
		public DocumentItem()
		{
			MaterialList = new();
		}

		public void Validate()
		{}
	}

	/// <summary>
	/// Вычисляемые свойства и методы
	/// </summary>
	public partial class DocumentItem
	{
		// Вычисляемое свойство для общей стоимости
		public decimal TotalPrice => Piece * Quantity * UnitPrice;

		/// <summary>
		/// Вид дерева
		/// </summary>
		public string? WoodType => ParseFromDescription("Вид дерева");

		/// <summary>
		/// Цвет покраски
		/// </summary>
		public string? Color => ParseFromDescription("Цвет");

		/// <summary>
		/// Цвет алюминия
		/// </summary>
		public string? ColorAl => ParseFromDescription("Цвет алюминия");

		/// <summary>
		/// Размеры ШхВ
		/// </summary>
		public string? Dimentions => ParseFromDescription("ШхВ");

		/// <summary>
		/// Площадь изделия
		/// </summary>
		public decimal Area => AreaByItemType();

		// Вспомогательное свойство для получения всех материалов одного типа
		public List<IMaterialItem> AllAdditionalMaterials 
		{ 
			get 
			{
				var items = new List<IMaterialItem>();
				
				if(MaterialList != null && MaterialList.AllMaterialItems != null && MaterialList.AllMaterialItems.Any())
				{
					items.AddRange(MaterialList.AllMaterialItems);
				}
				
				items.AddRange(Accessories);
				return items;
			}
		}

		// Вычисляемое свойство для проверки наличия материалов
		public bool HasMaterials 
		{ 
			get 
			{
				return 
					(MaterialList?.Paints != null && MaterialList.Paints.Any()) ||
					(MaterialList?.Fittings != null && MaterialList.Fittings.Any()) ||
					(MaterialList?.GlassProducts != null && MaterialList.GlassProducts.Any()) ||
					(MaterialList?.WoodProfiles != null && MaterialList.WoodProfiles.Any()) ||
					(MaterialList?.TechnicalArticles != null && MaterialList.TechnicalArticles.Any())||
					(MaterialList?.Accessories != null && MaterialList.Accessories.Any())
					|| (Accessories != null && Accessories.Any());
			}
		}

		/// <summary>
		/// Получение площади изделия из описания
		/// </summary>
		private decimal AreaByItemType()
		{
			switch (ItemType)
			{
				case DocumentItemType.Konstruktion: return ParseDecimalFromDescription("Площадь изделия");
				case DocumentItemType.GlassProduct: return ParseDecimalFromDescription("Площадь изделия");
				case DocumentItemType.MosquitoNet: return ParseDecimalFromDescription("Площадь изделия");
				case DocumentItemType.PanelProducts: return Quantity;
				default: return 0;
			}
			
		}

		private string? ParseFromDescription(string key)
		{
			if (string.IsNullOrWhiteSpace(Description)) return null;
			
			var pattern = $@"{key}:\s*([^|]+)";
			var match = Regex.Match(Description, pattern);
			return match.Success ? match.Groups[1].Value.Trim() : null;
		}

		private decimal ParseDecimalFromDescription(string key)
		{
			var value = ParseFromDescription(key);
			if (string.IsNullOrEmpty(value))
				return 0;
				
			if (decimal.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
				return result;
				
			return 0;
		}
		public bool DoubleColor()
		{
			// Если цвет null или пустой
			if (string.IsNullOrWhiteSpace(Color))
				return false;
			
			// Разделяем строку по "/"
			var colors = Color.Split('/', StringSplitOptions.RemoveEmptyEntries);
			
			// Если нет двух цветов
			if (colors.Length != 2)
				return false;
			
			// Если оба цвета есть и они разные
			return colors[0].Trim() != colors[1].Trim();
		}

		/// <summary>
		/// Заполнение всех материалов в один список с правильными типами
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items"></param>
		private void LoadMaterials<T>(IEnumerable<T>? items, MaterialTypeConfigService materialService) where T : IXmlDataMaterial
		{
			if (items == null) return;
			
			foreach (var item in items)
			{
				var material = Material.FromXmlDataMaterial(item, this, materialService);
				
				if(material != null)
				{
					Materials.Add(material);
				}
			}
		}

		public void LoadMaterials(XmlDocumentItem xmlData, MaterialTypeConfigService materialService)
		{
			Materials.Clear();
			
			// Пиломатериалы (нужно развернуть вложенные)
			if (xmlData.xmlMaterialList?.xmlWoodProfiles != null)
			{
				foreach (var woodProfile in xmlData.xmlMaterialList.xmlWoodProfiles)
				{
					LoadMaterials(woodProfile.xmlProfileDetailDatas, materialService);
				}
			}
			
			// Все остальные - напрямую
			LoadMaterials(xmlData.xmlMaterialList?.xmlPaints, materialService);
			LoadMaterials(xmlData.xmlMaterialList?.xmlFittings, materialService);
			LoadMaterials(xmlData.xmlMaterialList?.xmlGlassProducts, materialService);
			LoadMaterials(xmlData.xmlAccessories, materialService);
			LoadMaterials(xmlData.xmlMaterialList?.xmlTechnicalArticles, materialService);
		}
	}

	public static class DocumentItemExtensions
	{
		public static Dictionary<MaterialType, List<Material>> GroupByTypes(this DocumentItem documentItem)
		{
			return documentItem.Materials
				.Where(m => m.Type != null)
				.GroupBy(i => i.Type!)
				.ToDictionary(g => g.Key, g => g.ToList());
		}

		public static bool IsInCategory(this DocumentItem documentItem, IndicatorCategoryType category, SortingIndicatorService sortingIndicatorService)
		{
			return sortingIndicatorService.IsInCategory(documentItem.SortingIndicator ?? "", category);
		}

		public static List<IndicatorCategoryType> GetCategories(this DocumentItem documentItem, SortingIndicatorService sortingIndicatorService)
		{
			return sortingIndicatorService.GetCategoriesForIndicator(documentItem.SortingIndicator ?? "");
		}
	}
}