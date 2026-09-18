using System.Xml.Serialization;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models.Xml;

namespace KG.MES.Main.Models
{
	public class TechnicalArticle : IMaterialItem
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
		/// ????
		/// </summary>
		// TODO: 
		// FIXME: Что за поле <rodlength>, которое читается сюда
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
		/// Тип принадлежности - в составе позиции. Для разделения принадлежностей, добавленных к позиции, и самостоятельной позицией
		/// </summary>
		public AccessoryType Type { get; set; } = AccessoryType.ArtTech;

		public TechnicalArticle () {}

		public TechnicalArticle (XmlTechnicalArticle xmlTechnicalArticle)
		{
			ArticleNumber = xmlTechnicalArticle.ArticleNumber;
			Description = xmlTechnicalArticle.Name;
			Price = xmlTechnicalArticle.Price;
			Quantity = xmlTechnicalArticle.RodLength;
			Pieces = 1;
			Unit = xmlTechnicalArticle.Unit;
		}
	}
}