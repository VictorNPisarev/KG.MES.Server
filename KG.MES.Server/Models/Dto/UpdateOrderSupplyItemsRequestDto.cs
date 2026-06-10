using System.Text.Json.Serialization;

namespace KG.MES.Server.Models.Dto;

public class UpdateOrderSupplyItemsRequestDto
{
	[JsonPropertyName("supplies")]
	public List<UpdateSupplyItemRequest> Supplies { get; set; } = [];
}