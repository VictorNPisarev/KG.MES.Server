// Models/MaterialItem.cs
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using KG.MES.Main.Common.Extensions;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models.Enums;
using KG.MES.Main.Models.Xml;
using KG.MES.Main.Services;

namespace KG.MES.Main.Models
{
	/// <summary>
	/// Универсальный класс для любого материала в заказе
	/// </summary>
	public class Material : IMaterial
	{
		// Идентификатор
		public Guid Id { get; set; } = new();
		
		[JsonIgnore, XmlIgnore]
		public DocumentItem? DocumentItem { get; set; }
		
		// Категоризация
		public MaterialType? Type { get; set; }
		// Категоризация
		public string? TypeDefinition { get; set; }
		
		// Описание материала
		public string? Name { get; set; }           // Наименование (например, "Сосна А3 84х86")
		public string? Description { get; set; }    // Детальное описание
		public string? ArticleNumber { get; set; }  // Артикул (если есть)
		
		// Количество
		public decimal Quantity { get; set; }       // Основное количество
		public int PieceCount { get; set; }         // Количество штук
		public string? Unit { get; set; }           // Единица измерения (м, м², м³, кг, шт, Литры)
		
		// Цены
		public decimal UnitPrice { get; set; }      // Цена за единицу
		public decimal TotalPrice => Quantity * UnitPrice;
		
		// Дополнительные поля для специфических типов (хранятся в Dictionary или отдельных свойствах)
		public Dictionary<string, object> Attributes { get; set; } = new();
		
		// Удобное отображение количества
		public string QuantityDisplay => Quantity > 0 ? $"{Quantity:F2} {Unit}" : $"{PieceCount} {Unit}";
		
		// Конструктор
		public Material() { }

		public static Material? FromXmlDataMaterial(IXmlDataMaterial xmlMaterial, DocumentItem parent, MaterialTypeConfigService materialTypeConfigService)
		{
			var (typeKey, ruleUnit, coefficient, skip) = materialTypeConfigService.ResolveType(xmlMaterial);

			if (skip) return null;

			var unit = materialTypeConfigService.GetUnitForMaterial(typeKey, ruleUnit);
			var type = materialTypeConfigService.GetType(typeKey);

			var quantity = xmlMaterial.Quantity;
			var adjustedQuantity = (decimal)((double)quantity * coefficient);

			return new Material
			{
				DocumentItem = parent,
				Type = type,
				TypeDefinition = type?.Description ?? typeKey,
				Name = xmlMaterial.Name,
				Description = xmlMaterial.Name,
				ArticleNumber = xmlMaterial.ArticleNumber,
				Quantity = adjustedQuantity,
				PieceCount = xmlMaterial.PieceCount,
				Unit = unit ?? xmlMaterial.Unit,
				UnitPrice = xmlMaterial.UnitPrice
			};
		}
	}
}