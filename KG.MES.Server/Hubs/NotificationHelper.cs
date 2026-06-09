// Hubs/NotificationHelper.cs
using Microsoft.AspNetCore.SignalR;

namespace KG.MES.Server.Hubs;

public static class NotificationHelper
{
	private static IHubContext<NotificationHub>? _hubContext;

	public static void Initialize(IHubContext<NotificationHub> hubContext)
	{
		_hubContext = hubContext;
	}

	// order:updated (как в Socket.IO)
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

	// supply:updated
	public static async Task SupplyUpdated(Guid orderId, Guid supplyTypeId, Guid? conditionId)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"order_{orderId}").SendAsync("supply:updated", new
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
}