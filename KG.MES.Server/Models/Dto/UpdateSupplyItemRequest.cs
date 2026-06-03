// KG.MES.Shared/Models/Dto/SupplyTypeDto.cs
namespace KG.MES.Shared.Models.Dto;

public class UpdateSupplyItemRequest
{
	public Guid SupplyTypeId { get; set; }
	public Guid? SupplyConditionId { get; set; }
	public DateTime? ExpectedDate { get; set; }
	public decimal? Quantity { get; set; }
	public string? Comment { get; set; }
}