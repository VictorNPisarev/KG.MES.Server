using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class UpdateOrderSupplyItemsRequestDto
{
	[JsonPropertyName("supplies")]
	public List<UpdateSupplyItemRequest> Supplies { get; set; } = [];
}