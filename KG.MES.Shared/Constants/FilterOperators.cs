namespace KG.MES.Shared.Constants;

/// <summary>
/// Типы операторов для фильтрации
/// </summary>
public static class FilterOperators
{
	public const string Equal = "eq";
	public const string NotEqual = "ne";
	public const string Contains = "contains";
	public const string StartsWith = "startswith";
	public const string EndsWith = "endswith";
	public const string In = "in";
	public const string NotIn = "nin";
	public const string Between = "between";
	public const string GreaterThan = "gt";
	public const string LessThan = "lt";
	public const string GreaterThanOrEqual = "gte";
	public const string LessThanOrEqual = "lte";
	public const string IsNull = "isnull";
	public const string IsNotNull = "isnotnull";
}