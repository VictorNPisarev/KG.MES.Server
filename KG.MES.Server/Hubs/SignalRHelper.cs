// Hubs/NotificationHelper.cs
using Microsoft.AspNetCore.SignalR;

namespace KG.MES.Server.Hubs;

public static class SignalRHelper
{
	private static IHubContext<SignalRHub>? _hubContext;

	public static void Initialize(IHubContext<SignalRHub> hubContext)
	{
		_hubContext = hubContext;
	}

	// order:updated (как в Socket.IO)
	public static async Task OrderUpdated(Guid productionOrderId, Guid workplaceId, string newStatus)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"order_{productionOrderId}").SendAsync("order:updated", new
		{
			ProductionOrderId = productionOrderId,
			WorkplaceId = workplaceId,
			Status = newStatus,
			Timestamp = DateTime.UtcNow
		});
	}

	// workplace:order:updated
	public static async Task WorkplaceOrderUpdated(Guid workplaceId, Guid productionOrderId, string newStatus)
	{
		if (_hubContext == null) return;

		await _hubContext.Clients.Group($"workplace_{workplaceId}").SendAsync("workplace:order:updated", new
		{
			ProductionOrderId = productionOrderId,
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

	public static async Task OrderNotificationCreated(
	Guid orderId,
	string eventCode,
	string severity,
	Guid? roleId,
	Guid? userId)
	{
		if (_hubContext == null)
		{
			Console.WriteLine($":::: NotificationHelper :: OrderNotificationCreated ::::");
			return;
		}

		var payload = new
		{
			OrderId = orderId,
			EventCode = eventCode,
			Severity = severity,
			Timestamp = DateTime.UtcNow
		};

		// Если уведомление для конкретного пользователя
		if (userId.HasValue)
		{
			await _hubContext.Clients.User(userId.Value.ToString())
				.SendAsync("order-notification", payload);
		}
		// Если уведомление для роли
		else if (roleId.HasValue)
		{
			await _hubContext.Clients.Group($"role_{roleId}")
				.SendAsync("order-notification", payload);
		}
		// Если для всех
		else
		{
			await _hubContext.Clients.All
				.SendAsync("order-notification", payload);
		}
	}

	public static async Task OrderNotificationAvailable()
	{
		Console.WriteLine("========================================");
		Console.WriteLine("📡 NotificationHelper: МЕТОД ВЫЗВАН");

		if (_hubContext == null)
		{
			Console.WriteLine($":::: NotificationHelper :: NewOrdersAvailable :: _hubContext is null! Хаб не инициализирован. ::::");
			return;
		}

		Console.WriteLine("📡 NotificationHelper: Отправка new-orders-available всем...");
		try
		{
			await _hubContext.Clients.Group("notifications").SendAsync("new-orders-available");
			Console.WriteLine("✅ NotificationHelper: ОТПРАВЛЕНО УСПЕШНО");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"❌ NotificationHelper: ОШИБКА ОТПРАВКИ: {ex.Message}");
		}
		Console.WriteLine("========================================");
	}
}