using KG.MES.Server.Controllers;
using KG.MES.Server.Models.Dto;
using KG.MES.Shared.Models.Dto;

namespace KG.MES.Server.Services.Interfaces;

public interface IOrderService
{
	Task<PaginatedResponse<OrderListItemDto>> GetOrdersAsync(
		int page, int limit, string? sortBy, string? sortOrder, Guid? workplaceId, string? orderNumber);

	Task<OrderDetailDto?> GetOrderByIdAsync(Guid orderId);
	Task<OrderDetailDto?> GetOrderByNumberAsync(string orderNumber);
	Task<List<OrderTraceDto>> GetOrderTraceByNumberAsync(string orderNumber);
	Task<List<OrderWorkplaceDto>> GetPendingOrdersForWorkplaceAsync(Guid workplaceId);
	Task<List<OrderWorkplaceDto>> GetActiveOrdersForWorkplaceAsync(Guid workplaceId);
	Task<List<OrderWorkplaceDto>> GetActiveAndPendingOrdersForWorkplaceAsync(Guid workplaceId);
	Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderRequestDto request);
	Task<OperationResultDto> BeginOrderWorkplaceAsync(Guid productionOrderId, Guid workplaceId, Guid userId, string notes, string source);
	Task<OperationResultDto> CompleteOrderWorkplaceAsync(Guid productionOrderId, Guid workplaceId, Guid userId, string notes, string source);
	Task<SetFootprintResultDto> SetOrderFootprintStatusAsync(Guid productionOrderId, Guid workplaceId, string status, Guid? userId, string notes);
	Task<BatchUpdateResultDto> UpdateOrderFootprintBatchAsync(Guid productionOrderId, List<FootprintItemDto> footprints, Guid? userId, string notes);
	Task<List<OrderCommentDto>> GetOrderCommentsAsync(Guid orderId);
	Task<OrderCommentDto> AddOrderCommentAsync(Guid orderId, Guid? userId, string content);
	Task<OrderCommentDto> AddProductionOrderCommentAsync(Guid orderId, Guid productionOrderId, Guid? userId, string content);
	Task<OrderCommentDto> AddSupplyCommentAsync(Guid orderId, Guid supplyTypeId, Guid? userId, string content);
	Task<OrderCommentDto> UpdateOrderCommentAsync(Guid orderId, Guid commentId, string content);
}