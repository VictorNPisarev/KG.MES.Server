using Microsoft.AspNetCore.SignalR;

namespace KG.MES.Server.Hubs;

public class NotificationHub : Hub
{
	private readonly ILogger<NotificationHub> _logger;

	public NotificationHub(ILogger<NotificationHub> logger)
	{
		_logger = logger;
	}

	public override async Task OnConnectedAsync()
	{
		_logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		_logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
		await base.OnDisconnectedAsync(exception);
	}

	// Подписка на обновления заказа
	public async Task SubscribeToOrder(string orderId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
		_logger.LogInformation("Client {ConnectionId} subscribed to order {OrderId}", Context.ConnectionId, orderId);
	}

	// Отписка от обновлений заказа
	public async Task UnsubscribeFromOrder(string orderId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
		_logger.LogInformation("Client {ConnectionId} unsubscribed from order {OrderId}", Context.ConnectionId, orderId);
	}

	// Подписка на обновления участка
	public async Task SubscribeToWorkplace(string workplaceId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, $"workplace_{workplaceId}");
		_logger.LogInformation("Client {ConnectionId} subscribed to workplace {WorkplaceId}", Context.ConnectionId, workplaceId);
	}

	// Отписка от обновлений участка
	public async Task UnsubscribeFromWorkplace(string workplaceId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workplace_{workplaceId}");
		_logger.LogInformation("Client {ConnectionId} unsubscribed from workplace {WorkplaceId}", Context.ConnectionId, workplaceId);
	}

	// Подписка на обновления снабжения
	public async Task SubscribeToSupply()
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, "supply");
		_logger.LogInformation("Client {ConnectionId} subscribed to supply updates", Context.ConnectionId);
	}

	// Отписка от обновлений снабжения
	public async Task UnsubscribeFromSupply()
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, "supply");
		_logger.LogInformation("Client {ConnectionId} unsubscribed from supply updates", Context.ConnectionId);
	}
}