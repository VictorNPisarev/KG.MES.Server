using System.Xml.Serialization;
using KG.MES.Main.Interfaces;

namespace KG.MES.Main.Models.Xml
{
	public class Accessory : IMaterialItem
	{
		/// <summary>
		/// Артикул
		/// </summary>
		public string? ArticleNumber { get; set; }
		
		/// <summary>
		/// Описание артикула
		/// </summary>
		public string? Description { get; set; }
		
		/// <summary>
		/// Цена
		/// </summary>
		public decimal Price { get; set; }

		/// <summary>
		/// Количество экземпляров позиции
		/// </summary>
		public decimal Quantity { get; set; }
		
		/// <summary>
		/// Кол-во, шт
		/// </summary>
		public int Pieces { get; set; }
		
		/// <summary>
		/// Ед. измерения
		/// </summary>
		public string? Unit { get; set; }

		/// <summary>
		/// Тип принадлежности - самостоятельная позиции. Для разделения принадлежностей, добавленных к позиции, и самостоятельной позицией
		/// </summary>
		public AccessoryType Type { get; set; } = AccessoryType.AccessoryItem;
	
		public Accessory () {}

		public Accessory (XmlAccessory xmlAccessory)
		{
			ArticleNumber = xmlAccessory.ArticleNumber;
			Description = xmlAccessory.Name;
			Price = xmlAccessory.Price;
			Quantity = xmlAccessory.Quantity;
			Pieces = xmlAccessory.Piece;
			Unit = xmlAccessory.Unit;
		}

	}
}