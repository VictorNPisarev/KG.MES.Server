namespace KG.MES.Shared.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ColumnAttribute : Attribute
	{
		public string Title { get; }
		public bool Visible { get; set; } = true;
		public string? DisplayFormat { get; set; }
		public int Order { get; set; }
		public bool IsBadge { get; set; }
		public string? BadgeProperty { get; set; }
		public string? BadgeGroup { get; set; }
		public int Width { get; set; }
		public string? CommentField { get; set; }
		public string? DisplayGroup { get; set; }  // группа для поиска отображаемого текста в конфиге

		/// <summary>
		/// Аттрибут добавляющий иконку в поле по условному bool полю.
		/// </summary>
		public string[]? IconConditions { get; set; } // ["IsClaim:Claim", "IsEconom:Econom"]

		public bool ShowTotal { get; set; } = false; // true - если надо вывести сумму в подвале таблицы (используется при выводе таблицы через DynamicTable)

		public bool Sortable { get; set; } = false;

		public bool Filterable { get; set; } = false;


		public ColumnAttribute(string title)
		{
			Title = title;
		}
	}
}