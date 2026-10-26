namespace KG.MES.Shared.Models.Dto;

public class OrderNotificationDto
{
	public Guid Id { get; set; }
	public Guid OrderId { get; set; }
	public string EventType { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
}