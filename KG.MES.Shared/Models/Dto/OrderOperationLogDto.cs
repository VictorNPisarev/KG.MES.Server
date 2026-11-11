using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

/// <summary>
/// Лог операции по заказу (START / COMPLETE на конкретном участке).
/// </summary>
public class OrderOperationLogDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("workplace_id")]
	public Guid WorkplaceId { get; set; }

	[JsonPropertyName("workplace_name")]
	public string WorkplaceName { get; set; } = string.Empty;

	[JsonPropertyName("workplace_code")]
	public string? WorkplaceCode { get; set; }

	[JsonPropertyName("user_id")]
	public Guid? UserId { get; set; }

	[JsonPropertyName("user_name")]
	public string? UserName { get; set; }

	[JsonPropertyName("operation_type")]
	public string OperationType { get; set; } = string.Empty;

	[JsonPropertyName("operation_time")]
	public DateTime OperationTime { get; set; }

	[JsonPropertyName("notes")]
	public string? Notes { get; set; }

	[JsonPropertyName("source")]
	public string? Source { get; set; }
}