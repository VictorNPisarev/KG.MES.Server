using KG.MES.Shared.Models.Config;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.ViewModels;
using KG.MES.Shared.Services;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace KG.MES.Supply.Components.Pages;

public partial class Home
{
	[Inject] private ProductionApiService ApiService { get; set; } = null!;
	[Inject] private OrderViewSettings AppSettings { get; set; } = null!;

	private string Endpoint => AppSettings.ListEndpoint;
	private string CardEndpoint => AppSettings.CardEndpoint;


	private async Task<PaginatedResponse<SupplyViewModel>> LoadOrderViewModels(
		Guid? workplaceId, Guid[]? workplaceIds, string? orderNumber,
		int currentPage, int pageSize, string? sortBy, string? sortOrder, List<FilterCondition> filters)
	{
		var orders = await ApiService.GetOrdersAsync<SupplyDto>(
			endpoint: Endpoint,
			workplaceId: workplaceId,
			workplaceIds: workplaceIds,
			orderNumber: orderNumber,
			page: currentPage,
			limit: pageSize,
			sortBy: sortBy,
			sortOrder: sortOrder,
			filters: filters
		);

		return new PaginatedResponse<SupplyViewModel>
		{
			Data = orders.Data.Select(o => o.Adapt<SupplyViewModel>()).ToList(),
			Pagination = orders.Pagination
		};
	}

	private async Task<OrderViewModel?> LoadOrderViewModel(Guid orderId)
	{
		var orderDto = await ApiService.GetOrderByIdAsync<OrderDto>(CardEndpoint, orderId);

		if (orderDto != null)
		{
			return orderDto.Adapt<OrderViewModel>();
		}

		return null;
	}

}