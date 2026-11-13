using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

/// <summary>
/// Лёгкий DTO для выбора менеджера в UI.
/// Содержит только то, что нужно для отображения в select/автокомплите.
/// </summary>
public class SalesManagerDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;
}