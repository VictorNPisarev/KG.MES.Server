using System.Text.Json.Serialization;

namespace KG.MES.Shared.Helpers;

public class TableSettings
{
	[JsonPropertyName("columns")]
	public List<ColumnSetting> Columns { get; set; } = new();

	[JsonPropertyName("pageSize")]
	public int PageSize { get; set; } = 50;

	[JsonPropertyName("sortBy")]
	public string SortBy { get; set; } = "ready_date";

	[JsonPropertyName("sortOrder")]
	public string SortOrder { get; set; } = "asc";
}
