using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KG.MES.Shared.Interfaces; // Убедись, что namespace совпадает с твоим ISocketService

namespace KG.MES.Shared.Services; // Или KG.MES.Shared.Services, где у тебя лежат сервисы

public class SignalRService : ISocketService, IAsyncDisposable
{
	private readonly HubConnection _hubConnection;
	private readonly ILogger<SignalRService> _logger;
	private bool _isInitialized;

	public event Action<string, object>? OnMessage;

	public SignalRService(IConfiguration configuration, ILogger<SignalRService> logger)
	{
		_logger = logger;

		var serverUrl = configuration["SignalR:Url"] ?? "http://192.168.0.179:3031/notificationHub";

		_hubConnection = new HubConnectionBuilder()
			.WithUrl(serverUrl)
			.WithAutomaticReconnect()
			.Build();

		RegisterHandlers();
	}

	private void RegisterHandlers()
	{
		_hubConnection.On<object>("order:updated", (data) =>
		{
			_logger.LogInformation("Received order:updated");
			OnMessage?.Invoke("order", data);
		});

		_hubConnection.On<object>("workplace:order:updated", (data) =>
		{
			_logger.LogInformation("Received workplace:order:updated");
			OnMessage?.Invoke("workplace", data);
		});

		_hubConnection.On<object>("supply:updated", (data) =>
		{
			_logger.LogInformation("Received supply:updated");
			OnMessage?.Invoke("supply", data);
		});

		_hubConnection.On<object>("order:comment:added", (data) =>
		{
			_logger.LogInformation("Received order:comment:added");
			OnMessage?.Invoke("comment", data);
		});
	}

	// ✅ Реализация всех методов ISocketService

	public async Task ConnectAsync()
	{
		if (_hubConnection.State == HubConnectionState.Disconnected)
		{
			await _hubConnection.StartAsync();
			_isInitialized = true;
			_logger.LogInformation("SignalR connected");
		}
	}

	public async Task SubscribeAsync(string channel)
	{
		await SubscribeAsync(channel, null);
	}

	public async Task SubscribeAsync(string channel, string? id = null)
	{
		if (!_isInitialized) await ConnectAsync();

		switch (channel.ToLower())
		{
			case "order":
				if (!string.IsNullOrEmpty(id))
				{
					await _hubConnection.InvokeAsync("SubscribeToOrder", id);
					_logger.LogInformation("Subscribed to order {OrderId}", id);
				}
				break;
			case "workplace":
				if (!string.IsNullOrEmpty(id))
				{
					await _hubConnection.InvokeAsync("SubscribeToWorkplace", id);
					_logger.LogInformation("Subscribed to workplace {WorkplaceId}", id);
				}
				break;
			case "supply":
				await _hubConnection.InvokeAsync("SubscribeToSupply");
				_logger.LogInformation("Subscribed to supply");
				break;
		}
	}

	public async Task UnsubscribeAsync(string channel)
	{
		if (!_isInitialized) return;

		switch (channel.ToLower())
		{
			case "order":
				// Для простоты можно не передавать id, если хаб умеет отписывать по всем, 
				// но лучше передать, если он есть в контексте. Пока оставим базовый вызов.
				await _hubConnection.InvokeAsync("UnsubscribeFromOrder"); // Заглушка, адаптируй под свой хаб при необходимости
				break;
			case "supply":
				await _hubConnection.InvokeAsync("UnsubscribeFromSupply");
				break;
		}
	}

	public async Task JoinOrderAsync(string productionOrderId)
	{
		// JoinOrderAsync в старом коде был алиасом для подписки на заказ
		await SubscribeAsync("order", productionOrderId);
	}

	public async Task DisconnectAsync()
	{
		if (_hubConnection.State == HubConnectionState.Connected)
		{
			await _hubConnection.StopAsync();
			_isInitialized = false;
			_logger.LogInformation("SignalR disconnected");
		}
	}

	public async ValueTask DisposeAsync()
	{
		await DisconnectAsync();
		await _hubConnection.DisposeAsync();
	}
}