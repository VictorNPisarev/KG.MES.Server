using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class AddCommentRequestDto
{
	[JsonPropertyName("userId")]
	public Guid? UserId { get; set; }
	
	[JsonPropertyName("content")]
	public string Content { get; set; } = string.Empty;
}
