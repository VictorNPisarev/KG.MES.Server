using KG.MES.Shared.Interfaces;

namespace KG.MES.Shared.Services;

public class EventAggregator : IEventAggregator
{
	private readonly Dictionary<Type, List<object>> _handlers = [];

	public void Publish<TEvent>(TEvent eventData)
	{
		var type = typeof(TEvent);

		if (_handlers.ContainsKey(type))
		{
			foreach (var handler in _handlers[type].ToList())
			{
				((Action<TEvent>)handler).Invoke(eventData);
			}
		}
	}

	public void Subscribe<TEvent>(Action<TEvent> handler)
	{
		var type = typeof(TEvent);
		
		if (!_handlers.ContainsKey(type))
			_handlers[type] = [];

		_handlers[type].Add(handler);
	}

	public void Unsubscribe<TEvent>(Action<TEvent> handler)
	{
		var type = typeof(TEvent);
		if (_handlers.ContainsKey(type))
			_handlers[type].Remove(handler);
	}
}