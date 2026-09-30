using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.ViewModels;
using Microsoft.Extensions.Logging;

namespace KG.MES.Shared.Services;

public class SupplyService
{
	private readonly ProductionApiService api;
	private readonly ILogger<SupplyService> logger;

	private List<SupplyTypeDto>? cachedTypes;
	private List<SupplyConditionDto>? cachedConditions;

	public SupplyService(ProductionApiService api, ILogger<SupplyService> logger)
	{
		this.api = api;
		this.logger = logger;
	}

	public async Task<List<SupplyTypeDto>> GetTypesAsync()
	{
		if (cachedTypes != null) return cachedTypes;

		try
		{
			cachedTypes = await api.GetSupplyTypesAsync();
			return cachedTypes ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error loading supply types");
			return [];
		}
	}

	public async Task<List<SupplyConditionDto>> GetConditionsAsync()
	{
		if (cachedConditions != null) return cachedConditions;

		try
		{
			cachedConditions = await api.GetSupplyConditionsAsync();
			return cachedConditions ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error loading supply conditions");
			return [];
		}
	}
	public async Task<List<OrderSupplyViewModel>> GetOrderSuppliesAsync(Guid orderId)
	{
		var dtos = await api.GetOrderSuppliesAsync(orderId);
		var types = await GetTypesAsync();
		var conditions = await GetConditionsAsync();

		return dtos.Select(dto =>
		{
			var orderSupplyViewModel = new OrderSupplyViewModel(dto);

			orderSupplyViewModel.SupplyTypeName = types.FirstOrDefault(t => t.Id == orderSupplyViewModel.SupplyTypeId)?.DisplayName();
			
			var condition = conditions.FirstOrDefault(c => c.Id == orderSupplyViewModel.SupplyConditionId);
			
			orderSupplyViewModel.SupplyConditionName = condition?.DisplayName();
			orderSupplyViewModel.SupplyConditionCode = condition?.ConditionCode;
			
			return orderSupplyViewModel;
		}).ToList();
	}
}
