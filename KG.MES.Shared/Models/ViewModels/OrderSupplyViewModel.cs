using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.Shared.Models.ViewModels;

public class OrderSupplyViewModel
{
	public Guid SupplyTypeId { get; private set; }
	public Guid? SupplyConditionId { get; private set; }
	public string? Comment { get; set; }
	public Guid? CommentId { get; set; }

	// Справочные данные (заполняются извне)
	// Отображаемые данные (заполняются при маппинге извне)
	public string? SupplyTypeName { get; set; }
	public string? SupplyConditionName { get; set; }
	public string? SupplyConditionCode { get; set; }

	public OrderSupplyViewModel () {}

	public OrderSupplyViewModel(OrderSupplyDto orderSupplyDto)
	{
		orderSupplyDto.Adapt(this);
	}

	public OrderSupplyDto ToDto() => this.Adapt<OrderSupplyDto>();

	public void SetCondition(Guid conditionId, SupplyConditionDto? condition = null)
	{
		SupplyConditionId = conditionId;

		SupplyConditionName = condition?.DisplayName();
		SupplyConditionCode = condition?.ConditionCode;
	}
}