using Microsoft.AspNetCore.SignalR;

namespace KG.MES.Server.Hubs;

public class SignalRHub : Hub
{
	private readonly ILogger<SignalRHub> _logger;

	public SignalRHub(ILogger<SignalRHub> logger)
	{
		_logger = logger;
	}

	public override async Task OnConnectedAsync()
	{
		_logger.LogInformation($"Client connected: {Context.ConnectionId}");
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		_logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
		await base.OnDisconnectedAsync(exception);
	}

	// Подписка на обновления заказа
	public async Task SubscribeToOrder(string orderId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
		_logger.LogInformation($"Client {Context.ConnectionId} subscribed to order {orderId}");
	}

	// Подписка на обновления участка
	public async Task SubscribeToWorkplace(string workplaceId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, $"workplace_{workplaceId}");
		_logger.LogInformation($"Client {Context.ConnectionId} subscribed to workplace {workplaceId}");
	}

	// Подписка на обновления снабжения
	public async Task SubscribeToSupply()
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, "supply");
		_logger.LogInformation($"Client {Context.ConnectionId} subscribed to supply updates");
	}

	//Подписка на уведомления
	public async Task SubscribeToNotifications()
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, "notifications");
		_logger.LogInformation($"Client {Context.ConnectionId} subscribed to notifications");
		Console.WriteLine($"Client {Context.ConnectionId} subscribed to notifications");
	}

	// Отписка
	public async Task UnsubscribeFromOrder(string orderId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
	}

	public async Task UnsubscribeFromWorkplace(string workplaceId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workplace_{workplaceId}");
	}

	public async Task UnsubscribeFromSupply()
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, "supply");
	}

	public async Task UnsubscribeFromNotifications()
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, "notifications");
	}

	public async Task TestSendToMe()
	{
		Console.WriteLine("🔥 HUB: TestSendToMe ВЫЗВАН! Отправляем сообщение обратно...");
		await Clients.Caller.SendAsync("new-orders-available");
		Console.WriteLine("🔥 HUB: Сообщение ОТПРАВЛЕНО!");
	}
}