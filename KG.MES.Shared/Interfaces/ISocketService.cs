namespace KG.MES.Shared.Interfaces;

public interface ISocketService
{
	Task ConnectAsync();
	Task SubscribeAsync(string channel, string? id = null);
	Task SubscribeAsync(string channel);
	Task UnsubscribeAsync(string channel);
	event Action<string, object>? OnMessage;
	Task DisconnectAsync();
	Task JoinOrderAsync(string productionOrderId);
}
