using Microsoft.Extensions.Logging;
using KG.MES.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace KG.MES.Shared.Services
{
	public class SocketService : ISocketService
	{
		private readonly IJSRuntime _jsRuntime;
		private readonly ILogger<SocketService> _logger;
		private readonly string _serverUrl;
		private bool _isInitialized;

		public event Action<string, object>? OnMessage;

		public SocketService(IJSRuntime jsRuntime, IConfiguration configuration, ILogger<SocketService> logger)
		{
			_jsRuntime = jsRuntime;
			_logger = logger;
			_serverUrl = configuration["SocketIO:Url"] ?? "http://192.168.0.179:3000";
		}

		public async Task ConnectAsync()
		{
			await _jsRuntime.InvokeVoidAsync("socketService.connect", _serverUrl);
			await _jsRuntime.InvokeVoidAsync("socketService.onMessage", DotNetObjectReference.Create(this));
			_isInitialized = true;
		}

		[JSInvokable]
		public void OnSocketMessage(string channel, object data)
		{
			_logger.LogInformation("Socket message: {Channel}", channel);
			OnMessage?.Invoke(channel, data);
		}

		public async Task SubscribeAsync(string channel)
		{
			if (!_isInitialized) await ConnectAsync();
			await _jsRuntime.InvokeVoidAsync("socketService.subscribe", channel);
		}

		public async Task UnsubscribeAsync(string channel)
		{
			await _jsRuntime.InvokeVoidAsync("socketService.unsubscribe", channel);
		}

		public async Task DisconnectAsync()
		{
			await _jsRuntime.InvokeVoidAsync("socketService.disconnect");
			_isInitialized = false;
		}

		public async Task JoinOrderAsync(string productionOrderId)
		{
			await _jsRuntime.InvokeVoidAsync("socketService.joinOrder", productionOrderId);
		}

		public async Task SubscribeAsync(string channel, string? id = null)
		{
			await _jsRuntime.InvokeVoidAsync("socketService.subscribe", channel, id);
		}
	}
}