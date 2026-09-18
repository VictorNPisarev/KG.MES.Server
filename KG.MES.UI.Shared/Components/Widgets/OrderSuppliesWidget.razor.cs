using Microsoft.AspNetCore.Components;
using KG.MES.Shared.Models.ViewModels;
using KG.MES.Shared.Services;
using KG.MES.UI.Shared.Interfaces;
using KG.MES.Shared.Interfaces;
using KG.MES.Shared.Events;
using Microsoft.Extensions.Logging;
using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.UI.Shared.Components.Widgets;

public partial class OrderSuppliesWidget : ComponentBase, ISavableWidget
{
	[Parameter] public Guid OrderId { get; set; }
	[Parameter] public bool CanEdit { get; set; }

	[Inject] private ProductionApiService ApiService { get; set; } = null!;
	[Inject] private SupplyService SupplyService { get; set; } = null!;
	[Inject] private IEventAggregator EventAggregator { get; set; } = null!;
	[Inject] private ISocketService SocketService { get; set; } = null!;

	private List<OrderSupplyViewModel> supplies = [];
	private List<OrderSupplyViewModel> originalSupplies = [];
	private List<OrderSupplyViewModel> backup = [];
	private List<SupplyConditionDto> conditions = [];
	private List<SupplyTypeDto> types = [];

	private bool isLoading = true;
	private bool EditMode;
	private Dictionary<Guid, bool> showComments = [];
	private Dictionary<Guid, string> originalComments = [];
	private Dictionary<Guid, bool> openDropdowns = [];

	protected override async Task OnInitializedAsync()
	{
		await SocketService.SubscribeAsync("supply");
		SocketService.OnMessage += OnSocketMessage;

		//Подписываюсь на изменение комментария
		EventAggregator.Subscribe<OrderUpdatedEvent>(OnOrderCommentUpdated);

		conditions = await SupplyService.GetConditionsAsync();
		types = await SupplyService.GetTypesAsync();

		await LoadSupplies();

		isLoading = false;
	}

	private async Task LoadSupplies()
	{
		supplies = await SupplyService.GetOrderSuppliesAsync(OrderId);
		originalSupplies = supplies.Select(s => s.Adapt<OrderSupplyViewModel>()).ToList();

		StateHasChanged();
	}

	private void EnterEditMode()
	{
		backup = supplies.Select(s => s.Adapt<OrderSupplyViewModel>()).ToList();
		EditMode = true;
	}

	private void CancelEdit()
	{
		supplies = backup.ToList();
		backup.Clear();
		EditMode = false;
		showComments.Clear();
		originalComments.Clear();
		openDropdowns.Clear();
	}

	private void SelectCondition(OrderSupplyViewModel supply, SupplyConditionDto conditionDto)
	{
		supply.SetCondition(conditionDto.Id, conditionDto);
	}

	private void ToggleDropdown(Guid key)
	{
		var wasOpen = openDropdowns.GetValueOrDefault(key);
		openDropdowns.Clear();
		if (!wasOpen)
			openDropdowns[key] = true;
	}

	private void CloseDropdown(Guid key)
	{
		openDropdowns.Remove(key);
	}

	private void ToggleComment(OrderSupplyViewModel supply)
	{
		var key = supply.SupplyTypeId;
		if (showComments.GetValueOrDefault(key))
		{
			showComments.Remove(key);
		}
		else
		{
			originalComments[key] = supply.Comment ?? "";
			showComments[key] = true;
		}
	}

	public async Task SaveAllAsync()
	{
		if (EditMode)
		{
			await SaveChanges();
		}
	}

	private async Task SaveChanges()
	{
		var updates = new List<object>();

		foreach (var s in supplies)
		{
			var original = backup.FirstOrDefault(b => b.SupplyTypeId == s.SupplyTypeId);
			if (original != null)
			{
				if (s.SupplyConditionId != original.SupplyConditionId || s.Comment != original.Comment)
				{
					updates.Add(new
					{
						supplyTypeId = s.SupplyTypeId,
						supplyConditionId = s.SupplyConditionId,
						comment = s.Comment
					});
				}
			}

			if (showComments.GetValueOrDefault(s.SupplyTypeId))
			{
				var origComment = originalComments.GetValueOrDefault(s.SupplyTypeId) ?? "";
				if (s.Comment != origComment)
				{
					updates.Add(new
					{
						supplyTypeId = s.SupplyTypeId,
						supplyConditionId = s.SupplyConditionId,
						comment = s.Comment
					});
				}
			}
		}

		updates = updates
			.GroupBy(u => ((dynamic)u).supplyTypeId)
			.Select(g => g.First())
			.ToList();

		if (!updates.Any())
		{
			ClearState();
			return;
		}
		
		var success = await ApiService.UpdateOrderSuppliesAsync(OrderId, updates);

		if (success)
		{
			ClearState();
			supplies = await SupplyService.GetOrderSuppliesAsync(OrderId);
			originalSupplies = supplies.Select(s => s.Adapt<OrderSupplyViewModel>()).ToList(); 


			// Публикую событие
			EventAggregator.Publish(new OrderUpdatedEvent
			{
				OrderId = OrderId,
				Source = "supply"
			});
			//_ = Task.Run(() => EventAggregator.Publish(new OrderCommentUpdatedEvent
			//{
			//	OrderId = OrderId,
			//	Source = "supply"
			//}));
		}
		else
		{
			Console.Error.WriteLine("UpdateOrderSuppliesAsync fail");
		}
	}

	private async Task SaveComment(OrderSupplyViewModel supply)
	{
		var original = originalSupplies.FirstOrDefault(o => o.SupplyTypeId == supply.SupplyTypeId);
		var success = false;

		Console.WriteLine($"original?.Comment: {original?.Comment}");

		if (original == null)
		{
			Console.WriteLine("original == null");
			success = await ApiService.UpdateOrderSuppliesAsync(OrderId,
			[
				new
				{
					supplyTypeId = supply.SupplyTypeId,
					supplyConditionId = supply.SupplyConditionId,
					comment = supply.Comment
				}
			]);
		}
		else if (supply.Comment != original?.Comment && supply.CommentId != null)
		{
			Console.WriteLine("supply.Comment != original?.Comment && supply.CommentId != null");
			success = await ApiService.UpdateCommentAsync(OrderId,
			new OrderCommentViewModel
			{
				Id = supply.CommentId ?? new Guid(),
				Content = supply.Comment
			}
			);
		}

		Console.WriteLine($"success: {success}");

		if (success)
		{
			// Обновляю оригинал
			original?.Comment = supply.Comment;

			// Публикую событие
			EventAggregator.Publish(new OrderUpdatedEvent
			{
				OrderId = OrderId,
				Source = "supply"
			});
			//_ = Task.Run(() => EventAggregator.Publish(new OrderCommentUpdatedEvent
			//{
			//	OrderId = OrderId,
			//	Source = "supply"
			//}));
		}

		showComments.Remove(supply.SupplyTypeId);
	}

	public bool HasUnsavedChanges()
	{
		if (!EditMode) return false;

		foreach (var s in supplies)
		{
			var original = backup.FirstOrDefault(b => b.SupplyTypeId == s.SupplyTypeId);
			if (original == null) continue;
			if (s.SupplyConditionId != original.SupplyConditionId || s.Comment != original.Comment) return true;
		}

		return false;
	}

	private void ClearState()
	{
		EditMode = false;
		showComments.Clear();
		originalComments.Clear();
		openDropdowns.Clear();
		backup.Clear();
	}

	private async void OnOrderCommentUpdated(OrderUpdatedEvent eventData)
	{
		if (eventData.OrderId == OrderId)
		{
			await LoadSupplies();
			await InvokeAsync(StateHasChanged);
		}
	}

	private void OnSocketMessage(string channel, object data)
	{
		if (channel == "supply")
		{
			//var orderId = data.orderId;

		//if (orderId == OrderId)
		//		InvokeAsync(async () => await LoadSupplies());
		}
	}


	public void Dispose()
	{
		EventAggregator.Unsubscribe<OrderUpdatedEvent>(OnOrderCommentUpdated);
	}

}