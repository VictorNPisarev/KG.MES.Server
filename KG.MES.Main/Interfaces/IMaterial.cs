using KG.MES.Main.Models.Enums;

namespace KG.MES.Main.Models
{
	/// <summary>
	/// Базовый интерфейс для всех материалов, чтобы можно было работать с ними единообразно
	/// </summary>
	public interface IMaterial
	{
		public Guid Id { get; set; }
		MaterialType? Type { get; set; } // "Lumber", "Paint", "Fitting", "Glass" и т.д.
		DocumentItem? DocumentItem { get; set; } // Ссылка на родительскую позицию
		int PieceCount { get; set; } // Количество экземпляров (шт)
		decimal TotalPrice { get; } // Общая стоимость материала
	}
}