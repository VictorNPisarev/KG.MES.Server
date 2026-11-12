public class WorkplaceOperationLogsSummary
{
	public Guid WorkplaceId { get; set; }
	public string WorkplaceName { get; set; } = string.Empty;
	public string? WorkplaceCode { get; set; }
	public DateTime? StartedAt { get; set; }
	public DateTime? CompletedAt { get; set; }
	public string? StartedByUserName { get; set; }
	public string? CompletedByUserName { get; set; }
	public string? StartedNotes { get; set; }
	public string? CompletedNotes { get; set; }
	public bool InProgress => StartedAt.HasValue && !CompletedAt.HasValue;
}
