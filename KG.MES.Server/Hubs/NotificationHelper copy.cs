using Microsoft.AspNetCore.SignalR;

namespace KG.MES.Server.Hubs;

public static class NotificationHelper_copy
{
	private static IHubContext<NotificationHub>? _hubContext;

	public static void Initialize(IHubContext<NotificationHub> hubContext)
	{
		_hubContext = hubContext;
	}

	public static async Task NotifyOrderStatusChanged(Guid orderId, Guid workplaceId, string? oldStatus, string newStatus, Guid? userId, string notes = "")
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"order_{orderId}").SendAsync("OrderStatusChanged", new
		{
			OrderId = orderId,
			WorkplaceId = workplaceId,
			OldStatus = oldStatus,
			NewStatus = newStatus,
			UserId = userId,
			Notes = notes,
			Timestamp = DateTime.UtcNow
		});

		await _hubContext.Clients.Group($"workplace_{workplaceId}").SendAsync("WorkplaceOrderUpdated", new
		{
			OrderId = orderId,
			WorkplaceId = workplaceId,
			Status = newStatus,
			Action = "status_changed",
			Timestamp = DateTime.UtcNow
		});
	}

	public static async Task NotifySupplyStatusChanged(Guid orderId, Guid supplyTypeId, Guid? conditionId)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"order_{orderId}").SendAsync("SupplyStatusChanged", new
		{
			OrderId = orderId,
			SupplyTypeId = supplyTypeId,
			ConditionId = conditionId,
			Timestamp = DateTime.UtcNow
		});

		await _hubContext.Clients.Group("supply").SendAsync("SupplyUpdated", new
		{
			OrderId = orderId,
			SupplyTypeId = supplyTypeId,
			ConditionId = conditionId,
			Timestamp = DateTime.UtcNow
		});
	}

	public static async Task NotifyCommentAdded(Guid orderId, Guid commentId, string content, Guid? userId)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"order_{orderId}").SendAsync("CommentAdded", new
		{
			OrderId = orderId,
			CommentId = commentId,
			Content = content,
			UserId = userId,
			Timestamp = DateTime.UtcNow
		});
	}
}