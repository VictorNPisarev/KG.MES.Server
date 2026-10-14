// KG.MES.Shared/Models/Dto/SupplyTypeDto.cs
using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class UpdateSupplyItemRequest
{
	[JsonPropertyName("supplyTypeId")]
	public Guid SupplyTypeId { get; set; }

	[JsonPropertyName("supplyConditionId")]
	public Guid? SupplyConditionId { get; set; }

	[JsonPropertyName("expectedDate")]
	public DateTime? ExpectedDate { get; set; }

	[JsonPropertyName("quantity")]
	public decimal? Quantity { get; set; }

	[JsonPropertyName("comment")]
	public string? Comment { get; set; }

	[JsonPropertyName("userId")]
	public Guid? UserId { get; set; }
}