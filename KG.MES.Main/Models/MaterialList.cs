using System.Xml.Serialization;
using System.Collections.Generic;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models.Xml;
using System.Text.Json.Serialization;

namespace KG.MES.Main.Models
{
	/// <summary>
	/// Модель списка материалов
	/// </summary>
	public partial class MaterialList
	{
		/// <summary>
		/// Идентификатор
		/// </summary>
		public Guid Id { get; set; } = new();

		/// <summary>
		/// Количество единиц позиции
		/// </summary>
		public int QuantityWindows { get; set; }

		/// <summary>
		/// Количество заполнений
		/// </summary>
		public int QuantityGlasses { get; set; }
		
		/// <summary>
		/// Список ЛКМ
		/// </summary>
		public List<Paint> Paints { get; set; } = new();

		/// <summary>
		/// Список фурнитуры
		/// </summary>
		public List<Fitting> Fittings { get; set; } = new();
		
		/// <summary>
		/// Список заполнений
		/// </summary>
		public List<GlassProduct> GlassProducts { get; set; } = new();
		
		/// <summary>
		/// Список дерева
		/// </summary>
		public List<WoodProfile> WoodProfiles { get; set; } = new();

		/// <summary>
		/// Список бруса
		/// </summary>
		public List<ProfileDetailData> Lumbers { get; set; } = new();

		/// <summary>
		/// Технические артикулы
		/// </summary>
		public List<TechnicalArticle> TechnicalArticles { get; set; } = new();

		/// <summary>
		/// Аксессуары
		/// </summary>
		public List<Accessory> Accessories { get; set; } = new();

		/// <summary>
		/// Ссылка на позицию
		/// </summary>
		[JsonIgnore, XmlIgnore]
		public DocumentItem? documentItem { get; set; }

		public MaterialList()
		{
			
		}

		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="xmlMaterialList"></param>
		/// <param name="documentItem">Ссылка на позицию</param>
		public MaterialList(XmlMaterialList xmlMaterialList, DocumentItem? documentItem = null)
		{
			this.documentItem = documentItem; //Ссылка на позицию

			QuantityWindows = xmlMaterialList.QuantityWindows;
			QuantityGlasses = xmlMaterialList.QuantityGlasses;

			foreach(var paint in xmlMaterialList.xmlPaints)
				this.Paints.Add(new Paint(paint));
			
			foreach(var fitting in xmlMaterialList.xmlFittings)
				this.Fittings.Add(new Fitting(fitting));
			
			foreach(var glass in xmlMaterialList.xmlGlassProducts)
				this.GlassProducts.Add(new GlassProduct(glass));
			
			foreach(var wood in xmlMaterialList.xmlWoodProfiles)
				this.WoodProfiles.Add(new WoodProfile(wood));

			foreach(var article in xmlMaterialList.xmlTechnicalArticles)
				this.TechnicalArticles.Add(new TechnicalArticle(article));

			foreach(var accessory in xmlMaterialList.xmlAccessories)
				this.Accessories.Add(new Accessory(accessory));

			foreach(var wood in WoodProfiles)
				this.Lumbers.AddRange(wood.profileDetailDatas);
		}

	}

	/// <summary>
	/// Вычисляемые и вспомогательные свойства
	/// </summary>
	public partial class MaterialList
	{
		// Вспомогательное свойство для получения всех материалов одного типа
		public List<IMaterialItem> AllMaterialItems 
		{ 
			get 
			{
				var items = new List<IMaterialItem>();
				items.AddRange(TechnicalArticles);
				items.AddRange(Accessories);
				return items;
			}
		}
	}
}