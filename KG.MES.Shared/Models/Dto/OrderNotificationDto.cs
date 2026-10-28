namespace KG.MES.Shared.Models.Dto;

public class OrderNotificationDto
{
	public Guid Id { get; set; }
	public Guid OrderId { get; set; }
	public string EventType { get; set; } = string.Empty;
	public string Severity { get; set; } = "Info";
	public DateTime CreatedAt { get; set; }
}