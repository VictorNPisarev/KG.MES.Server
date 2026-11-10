using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class UpdateSubscriptionRequestDto
{
	[JsonPropertyName("severity")]
	public string Severity { get; set; } = "Info";
}