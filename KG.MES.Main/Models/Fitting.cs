using System.Xml.Serialization;
using KG.MES.Main.Models.Xml;

namespace KG.MES.Main.Models
{
	public class Fitting
	{
		/// <summary>
		/// Артикул фурнитуры
		/// </summary>
		public string? FittingNumber { get; set; }

		/// <summary>
		/// Описание
		/// </summary>
		public string? Description { get; set; }
		
		/// <summary>
		/// Количество
		/// </summary>
		public int Piece { get; set; }
		
		/// <summary>
		/// Цена
		/// </summary>
		public decimal Price { get; set; }

		public Fitting () {}

		public Fitting (XmlFitting xmlFitting)
		{
			FittingNumber = xmlFitting.FittingNumber;
			Description = xmlFitting.Name;
			Piece = xmlFitting.PieceCount;
			Price = xmlFitting.Price ?? 0m;
		}
	}
}
