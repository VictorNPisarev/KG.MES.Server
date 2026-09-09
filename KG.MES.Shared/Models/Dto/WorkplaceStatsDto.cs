using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class WorkplaceStatsDto
{
	[JsonPropertyName("pending_count")]
	public int PendingCount { get; set; }

	[JsonPropertyName("joinery_count")]
	public int JoineryCount { get; set; }

	[JsonPropertyName("active_count")]
	public int ActiveCount { get; set; }

	[JsonPropertyName("completed_count")]
	public int CompletedCount { get; set; }

	[JsonPropertyName("active_blocks")]
	public int ActiveBlocks { get; set; }

	[JsonPropertyName("active_orders")]
	public List<ActiveOrderDto> ActiveOrders { get; set; } = [];
}