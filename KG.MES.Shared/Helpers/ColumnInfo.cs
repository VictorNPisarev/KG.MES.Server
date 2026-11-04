namespace KG.MES.Shared.Helpers;

public class ColumnInfo
{
	public string PropertyName { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string? DisplayFormat { get; set; }
	public bool IsBadge { get; set; }
	public string? BadgeProperty { get; set; } // если IsBadge и значение берется из другого свойства
	public string? BadgeGroup { get; set; }
	public string? CommentField { get; set; }
	public string? DisplayGroup { get; set; }  // группа для поиска отображаемого текста в конфиге
	public string[]? IconConditions { get; set; }
	public bool ShowTotal { get; set; }
	public bool Sortable { get; set; }
	public bool Filterable { get; set; }
}
