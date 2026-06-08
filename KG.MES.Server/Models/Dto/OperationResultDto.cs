using System.Text.Json.Serialization;

namespace KG.MES.Server.Models.Dto;

public class OperationResultDto
{
	[JsonPropertyName("success")]
	public bool Success { get; set; }

	[JsonPropertyName("message")]
	public string Message { get; set; } = string.Empty;
}