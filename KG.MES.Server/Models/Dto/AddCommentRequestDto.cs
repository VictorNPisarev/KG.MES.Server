namespace KG.MES.Shared.Models.Dto;

public class AddCommentRequestDto
{
	public Guid? UserId { get; set; }
	public string Content { get; set; } = string.Empty;
}
