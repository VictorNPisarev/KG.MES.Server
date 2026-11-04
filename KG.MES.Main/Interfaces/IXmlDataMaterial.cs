using KG.MES.Main.Models.Enums;

namespace KG.MES.Main.Interfaces
{
	public interface IXmlDataMaterial
	{
		/// <summary>
		/// Тип XML-блока (XmlWoodProfile, XmlPaint, XmlFitting и т.д.)
		/// </summary>
		XmlBlockType XmlBlockType { get; }

		/// <summary>
		/// Артикул материала
		/// </summary>
		string? ArticleNumber { get; }
		
		/// <summary>
		/// Название материала
		/// </summary>
		string? Name { get; }

		/// <summary>
		/// Основное количество
		/// </summary>
		public decimal Quantity { get; }

		/// <summary>
		/// Количество штук
		/// </summary>
		public int PieceCount { get; }

		/// <summary>
		/// Единица измерения
		/// </summary>
		public string? Unit { get; }
		
		/// <summary>
		/// Цена за единицу
		/// </summary>
		public decimal UnitPrice { get; }

		/// <summary>
		/// Какие-либо специфические аттрибуты материала
		/// </summary>
		/// <returns></returns>
		Dictionary<string, object> GetAttributes() => new();

		void Validate();
	}
}