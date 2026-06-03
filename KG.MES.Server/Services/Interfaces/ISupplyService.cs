// KG.MES.Server/Services/Interfaces/ISupplyService.cs
using KG.MES.Shared.Models.Dto;

namespace KG.MES.Server.Services.Interfaces;

public interface ISupplyService
{
	Task<List<SupplyTypeDto>> GetSupplyTypesAsync();
	Task<List<SupplyConditionDto>> GetSupplyConditionsAsync();
	Task<List<OrderSupplyItemDto>> GetOrderSupplyItemsAsync(Guid orderId);
	Task<PaginatedResponse<SupplyStatusListItemDto>> GetAllSupplyItemsAsync(int page, int limit, string? orderNumber);
	Task<OperationResultDto> UpdateSupplyItemAsync(Guid orderId, Guid supplyTypeId, UpdateSupplyItemRequest request);
	Task<OperationResultDto> UpdateAllSupplyItemsAsync(Guid orderId, List<UpdateSupplyItemRequest> updates);
}