using Microsoft.AspNetCore.SignalR;

namespace KG.MES.Server.Hubs;

public static class NotificationHelper
{
	private static IHubContext<NotificationHub>? _hubContext;

	public static void Initialize(IHubContext<NotificationHub> hubContext)
	{
		_hubContext = hubContext;
	}

	// order:updated / order:status:changed
	public static async Task OrderUpdated(Guid orderId, Guid workplaceId, string newStatus, Guid? userId, string notes = "")
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"order_{orderId}").SendAsync("order:updated", new
		{
			ProductionOrderId = orderId,
			WorkplaceId = workplaceId,
			Status = newStatus,
			UserId = userId,
			Notes = notes,
			Timestamp = DateTime.UtcNow
		});
	}

	// order:completed
	public static async Task OrderCompleted(Guid orderId, Guid workplaceId)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"order_{orderId}").SendAsync("order:completed", new
		{
			ProductionOrderId = orderId,
			WorkplaceId = workplaceId,
			Timestamp = DateTime.UtcNow
		});
	}

	// workplace:order:updated
	public static async Task WorkplaceOrderUpdated(Guid workplaceId, Guid orderId, string newStatus)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"workplace_{workplaceId}").SendAsync("workplace:order:updated", new
		{
			ProductionOrderId = orderId,
			WorkplaceId = workplaceId,
			Status = newStatus,
			Action = "status_changed",
			Timestamp = DateTime.UtcNow
		});
	}

	// workplace:order:started
	public static async Task WorkplaceOrderStarted(Guid workplaceId, Guid orderId)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"workplace_{workplaceId}").SendAsync("workplace:order:started", new
		{
			ProductionOrderId = orderId,
			WorkplaceId = workplaceId,
			Timestamp = DateTime.UtcNow
		});
	}

	// workplace:order:completed
	public static async Task WorkplaceOrderCompleted(Guid workplaceId, Guid orderId)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"workplace_{workplaceId}").SendAsync("workplace:order:completed", new
		{
			ProductionOrderId = orderId,
			WorkplaceId = workplaceId,
			Timestamp = DateTime.UtcNow
		});
	}

	// supply:status:changed
	public static async Task SupplyStatusChanged(Guid orderId, Guid supplyTypeId, Guid? conditionId)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"order_{orderId}").SendAsync("supply:status:changed", new
		{
			OrderId = orderId,
			SupplyTypeId = supplyTypeId,
			ConditionId = conditionId,
			Timestamp = DateTime.UtcNow
		});
	}

	// supply:updated
	public static async Task SupplyUpdated(Guid orderId, Guid supplyTypeId, Guid? conditionId)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group("supply").SendAsync("supply:updated", new
		{
			OrderId = orderId,
			SupplyTypeId = supplyTypeId,
			ConditionId = conditionId,
			Timestamp = DateTime.UtcNow
		});
	}

	// order:comment:added
	public static async Task OrderCommentAdded(Guid orderId, Guid commentId, string content, Guid? userId)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"order_{orderId}").SendAsync("order:comment:added", new
		{
			OrderId = orderId,
			CommentId = commentId,
			Content = content,
			UserId = userId,
			Timestamp = DateTime.UtcNow
		});
	}

	// === Комбинированные методы для удобства ===

	public static async Task NotifyOrderStatusChanged(Guid orderId, Guid workplaceId, string oldStatus, string newStatus, Guid? userId, string notes = "")
	{
		await OrderUpdated(orderId, workplaceId, newStatus, userId, notes);
		await WorkplaceOrderUpdated(workplaceId, orderId, newStatus);

		if (newStatus == "completed")
		{
			await OrderCompleted(orderId, workplaceId);
			await WorkplaceOrderCompleted(workplaceId, orderId);
		}
		else if (newStatus == "active")
		{
			await WorkplaceOrderStarted(workplaceId, orderId);
		}
	}
}