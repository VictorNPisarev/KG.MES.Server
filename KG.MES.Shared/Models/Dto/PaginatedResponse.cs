namespace KG.MES.Shared.Models.Dto;

public class PaginatedResponse<T>
{
	public List<T> Data { get; set; } = new();
	public PaginationInfo Pagination { get; set; } = new();
	public SortInfo Sort { get; set; } = new();
	public FilterInfo Filters { get; set; } = new();
}

public class PaginationInfo
{
	public int Page { get; set; }
	public int Limit { get; set; }
	public int Total { get; set; }
	public int Pages { get; set; }
}

public class SortInfo
{
	public string By { get; set; } = string.Empty;
	public string Order { get; set; } = string.Empty;
}

public class FilterInfo
{
	public Guid? WorkplaceId { get; set; }
	public string? OrderNumber { get; set; }
}