namespace KG.MES.Shared.Models.Dto;

public class FilterFacetsRequestDto
{
	/// <summary>
	/// Список полей, для которых нужно получить уникальные значения
	/// </summary>
	public List<string> Fields { get; set; } = new();

	/// <summary>
	/// Опционально: фильтры, которые уже применены (чтобы фасеты учитывали их)
	/// </summary>
	public List<FilterCondition>? AppliedFilters { get; set; }
}