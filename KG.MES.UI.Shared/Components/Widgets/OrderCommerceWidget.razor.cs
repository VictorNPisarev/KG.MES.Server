using KG.MES.Shared.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.ViewModels;
using KG.MES.Shared.Services;
using KG.MES.UI.Shared.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace KG.MES.UI.Shared.Components.Widgets;

public partial class OrderCommerceWidget : ComponentBase, ISavableWidget
{
	[Parameter] public Guid OrderId { get; set; }

	[Inject] private ProductionApiService ApiService { get; set; } = null!;
	[Inject] private ISocketService SocketService { get; set; } = null!;
	[Inject] private IEventAggregator EventAggregator { get; set; } = null!;

	private CommerceOrderViewModel? commerceOrder;
	public bool isLoading = true;

	protected override async Task OnInitializedAsync()
	{
		var commerceOrderDto = await ApiService.GetCommerceAsync(OrderId);
		commerceOrder = commerceOrderDto.Adapt<CommerceOrderViewModel>();
		
		isLoading = false;
	}

	public bool HasUnsavedChanges() => false;
	public Task SaveAllAsync() => Task.CompletedTask;
}