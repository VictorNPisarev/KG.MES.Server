namespace KG.MES.Shared.Interfaces;

public interface IEventAggregator
{
	void Publish<TEvent>(TEvent eventData);
	void Subscribe<TEvent>(Action<TEvent> handler);
	void Unsubscribe<TEvent>(Action<TEvent> handler);
}
