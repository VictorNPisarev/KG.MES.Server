using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

/// <summary>
/// Условие фильтрации для динамической фильтрации данных
/// </summary>
public class FilterCondition
{
	/// <summary>
	/// Имя поля, по которому выполняется фильтрация
	/// </summary>
	[JsonPropertyName("field")]
	public string Field { get; set; } = string.Empty;

	/// <summary>
	/// Одиночное значение для фильтрации (используется для точного сравнения)
	/// </summary>
	[JsonPropertyName("value")]
	public object? Value { get; set; }

	/// <summary>
	/// Список значений для фильтрации (используется для IN-операций)
	/// </summary>
	[JsonPropertyName("values")]
	public List<object>? Values { get; set; }

	/// <summary>
	/// Оператор сравнения: "eq", "contains", "in", "between", "gt", "lt", "gte", "lte"
	/// </summary>
	[JsonPropertyName("operator")]
	public string? Operator { get; set; }
}