using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KG.MES.Shared.Interfaces;
using Microsoft.JSInterop; // Убедись, что namespace совпадает с твоим ISocketService

namespace KG.MES.Shared.Services; // Или KG.MES.Shared.Services, где у тебя лежат сервисы

public class SignalRService : ISocketService, IAsyncDisposable
{
	private readonly HubConnection hubConnection;
	private readonly NotificationStateService state;
	private readonly ILogger<SignalRService> logger;
	private bool isConnected;

	public event Action<string, object?>? OnMessage;

	public static string GlobalDebugLog { get; set; } = "";
	public static string GlobalLog { get; private set; } = "";

	public SignalRService(NotificationStateService state, IConfiguration configuration, ILogger<SignalRService> logger)
	{
		this.state = state;
		this.logger = logger;

		var serverUrl = configuration["SignalR:Url"] ?? "http://192.168.0.179:3031/notificationHub";

		hubConnection = new HubConnectionBuilder()
			.WithUrl(serverUrl)
			.WithAutomaticReconnect()
			.ConfigureLogging(logging =>
			{
				logging.SetMinimumLevel(LogLevel.Trace); // ← Включаем ВСЁ
				logging.AddConsole(); // ← Выводим в консоль браузера
			})
			.Build();

		RegisterHandlers();
	}

	private void RegisterHandlers()
	{
		hubConnection.On<object>("order:updated", (data) =>
		{
			logger.LogInformation("Received order:updated");
			Log("📡 Получено order:updated");
			OnMessage?.Invoke("order", data);
		});

		hubConnection.On<object>("workplace:order:updated", (data) =>
		{
			logger.LogInformation("Received workplace:order:updated");
			Log("📡 Получено workplace:order:updated");
			OnMessage?.Invoke("workplace", data);
		});

		hubConnection.On<object>("supply:updated", (data) =>
		{
			logger.LogInformation("Received supply:updated");
			Log("📡 Получено supply:updated");
			OnMessage?.Invoke("supply", data);
		});

		hubConnection.On<object>("order:comment:added", (data) =>
		{
			logger.LogInformation("Received order:comment:added");
			Log("📡 Получено order:comment:added");
			OnMessage?.Invoke("comment", data);
		});

		//_hubConnection.On<object>("new-orders-available", (data) =>
		//{
		//	_logger.LogInformation("Received new-orders-available");
		//	OnMessage?.Invoke("new-orders", data);
		//});
		hubConnection.On<object>("new-orders-available", async (data) =>
		{
			// Пишем НАПРЯМУЮ в консоль браузера

			GlobalDebugLog += $":::: [SignalRService] Получено new-orders-available в {DateTime.Now:HH:mm:ss}\n ::::";
			Log("📡 Получено new-orders-available");
			OnMessage?.Invoke("orderNotification", data);
		});
	}

	// ✅ Реализация всех методов ISocketService

	public async Task ConnectAsync()
	{
		if (!isConnected)
		{
			await hubConnection.StartAsync();
			isConnected = true;
			logger.LogInformation("SignalR connected");
		}
	}

	public async Task SubscribeAsync(string channel)
	{
		await SubscribeAsync(channel, null);
	}

	public async Task SubscribeAsync(string channel, string? id = null)
	{
		if (!isConnected) await ConnectAsync();

		switch (channel.ToLower())
		{
			case "order":
				if (!string.IsNullOrEmpty(id))
				{
					await hubConnection.InvokeAsync("SubscribeToOrder", id);
					logger.LogInformation("Subscribed to order {OrderId}", id);
				}
				break;
			case "workplace":
				if (!string.IsNullOrEmpty(id))
				{
					await hubConnection.InvokeAsync("SubscribeToWorkplace", id);
					logger.LogInformation("Subscribed to workplace {WorkplaceId}", id);
				}
				break;
			case "supply":
				await hubConnection.InvokeAsync("SubscribeToSupply");
				logger.LogInformation("Subscribed to supply");
				break;
			case "notifications":
				await hubConnection.InvokeAsync("SubscribeToNotifications");
				logger.LogInformation("Subscribed to notifications");
				break;
		}
	}

	public async Task UnsubscribeAsync(string channel)
	{
		if (!isConnected) return;

		switch (channel.ToLower())
		{
			case "order":
				// Для простоты можно не передавать id, если хаб умеет отписывать по всем, 
				// но лучше передать, если он есть в контексте. Пока оставим базовый вызов.
				await hubConnection.InvokeAsync("UnsubscribeFromOrder"); // Заглушка, адаптируй под свой хаб при необходимости
				break;
			case "workplace":
				await hubConnection.InvokeAsync("UnsubscribeFromWorkplace");
				break;
			case "supply":
				await hubConnection.InvokeAsync("UnsubscribeFromSupply");
				break;
			case "notifications":
				await hubConnection.InvokeAsync("UnsubscribeFromNotifications");
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
		if (hubConnection.State == HubConnectionState.Connected)
		{
			await hubConnection.StopAsync();
			isConnected = false;
			logger.LogInformation("SignalR disconnected");
		}
	}

	public async ValueTask DisposeAsync()
	{
		await DisconnectAsync();
		await hubConnection.DisposeAsync();
	}

	public async Task TestSendToMeAsync()
	{
		if (!isConnected) await ConnectAsync();
		await hubConnection.InvokeAsync("TestSendToMe");
	}

	private void Log(string message)
	{
		GlobalLog += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
	}
}