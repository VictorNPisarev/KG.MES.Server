using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class OrderCommentDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("content")]
	public string Content { get; set; } = string.Empty;

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("updated_at")]
	public DateTime? UpdatedAt { get; set; }

	[JsonPropertyName("user_id")]
	public Guid? UserId { get; set; }

	[JsonPropertyName("user_name")]
	public string? UserName { get; set; }

	[JsonPropertyName("comment_type")]
	public string? CommentType { get; set; }

	[JsonPropertyName("material_name")]
	public string? MaterialName { get; set; }
}