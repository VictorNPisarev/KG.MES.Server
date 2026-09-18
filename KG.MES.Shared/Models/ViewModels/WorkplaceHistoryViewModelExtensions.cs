using KG.MES.Shared.Models.Dto;

namespace KG.MES.Shared.Models.ViewModels;

public static class WorkplaceHistoryViewModelExtensions
{
	public static List<WorkplaceHistoryViewModel> CreateWithTransferIndicators(
		this List<WorkplaceHistoryViewModel> list,
		IEnumerable<WorkplaceHistoryDto> dtos,
		DateTime startPeriod,
		DateTime endPeriod)
	{
		var viewModels = dtos.Select(d => new WorkplaceHistoryViewModel(d)).ToList();

		foreach (var vm in viewModels)
		{
			if (vm.OperationType == Constants.OrderStatus.OperationType.Start)
			{
				var completeOp = viewModels.FirstOrDefault(o =>
					o.OrderNumber == vm.OrderNumber &&
					o.OperationType == Constants.OrderStatus.OperationType.Complete);
				
				vm.IsCompleteTransfer = completeOp == null || completeOp.OperationTime > endPeriod;
			}
			else if (vm.OperationType == Constants.OrderStatus.OperationType.Complete)
			{
				var startOp = viewModels.FirstOrDefault(o =>
					o.OrderNumber == vm.OrderNumber &&
					o.OperationType == Constants.OrderStatus.OperationType.Start);
				vm.IsStartTransfer = startOp == null || startOp.OperationTime < startPeriod;
			}
		}

		return viewModels;
	}
}