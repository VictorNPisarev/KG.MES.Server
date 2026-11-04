namespace KG.MES.Shared.Models.Dto;

public class OrderNotificationDto
{
	public Guid Id { get; set; }
	public Guid OrderId { get; set; }
	public string EventCode { get; set; } = string.Empty;
	public string EventDescription { get; set; } = string.Empty;
	public string Severity { get; set; } = "Info";
	public DateTime NotificationCreatedAt { get; set; }
}