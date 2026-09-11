namespace KG.MES.Shared.Models.Dto;

public class FilterFacetsResponseDto
{
	/// <summary>
	/// Словарь: имя поля → список уникальных значений
	/// </summary>
	public Dictionary<string, List<FacetValueDto>> Facets { get; set; } = new();
}

public class FacetValueDto
{
	public string Value { get; set; } = string.Empty;
	public int Count { get; set; }      // сколько записей с этим значением (опционально)
}