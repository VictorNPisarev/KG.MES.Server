using System.Text.Json.Serialization;

public class NotificationGroupDto<T>
{
	[JsonPropertyName("event_code")]
	public string EventCode { get; set; } = string.Empty;         // "NEW_ORDER", "ORDER_READY"

	[JsonPropertyName("event_description")]
	public string EventDescription { get; set; } = string.Empty;  // "Создан новый заказ"

	[JsonPropertyName("severity")]
	public string Severity { get; set; } = "Info";                // "Danger", "Warning", "Success"

	[JsonPropertyName("items")]
	public List<T> Items { get; set; } = new();                   // список заказов/элементов
}