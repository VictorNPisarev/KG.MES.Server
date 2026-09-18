namespace KG.MES.Shared.Events;

public class OrderUpdatedEvent
{
	public Guid OrderId { get; set; }
	public string? Source { get; set; } // 'order', 'production', 'supply'
}