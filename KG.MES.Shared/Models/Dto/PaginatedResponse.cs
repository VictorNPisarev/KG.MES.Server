using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class PaginatedResponse<T>
{
	[JsonPropertyName("data")]
	public List<T> Data { get; set; } = [];

	[JsonPropertyName("pagination")]
	public PaginationInfo Pagination { get; set; } = new();

	[JsonPropertyName("sort")]
	public SortInfo Sort { get; set; } = new();

	[JsonPropertyName("filters")]
	public FilterInfo Filters { get; set; } = new();

	[JsonPropertyName("totals")]
	public ITotalsDto? Totals { get; set; }

	// Удобные свойства для UI
	public int Page => Pagination.Page;
	public int Limit => Pagination.Limit;
	public int Total => Pagination.Total;
	public int TotalPages => Pagination.Pages;
	public bool HasNextPage => Page < TotalPages;
	public bool HasPreviousPage => Page > 1;

}

public class PaginationInfo
{
	[JsonPropertyName("page")]
	public int Page { get; set; }

	[JsonPropertyName("limit")]
	public int Limit { get; set; }

	[JsonPropertyName("total")]
	public int Total { get; set; }

	[JsonPropertyName("pages")]
	public int Pages { get; set; }
}

public class SortInfo
{
	[JsonPropertyName("by")]
	public string By { get; set; } = string.Empty;
	
	[JsonPropertyName("order")]
	public string Order { get; set; } = string.Empty;
}

public class FilterInfo
{
	[JsonPropertyName("workplace_id")]
	public Guid? WorkplaceId { get; set; }
	
	[JsonPropertyName("order_number")]
	public string? OrderNumber { get; set; }
}