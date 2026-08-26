namespace KG.MES.Shared.Models.Dto;


public class AddSupplyCommentRequestDto
{
	public Guid SupplyTypeId { get; set; }
	public Guid? UserId { get; set; }
	public string Content { get; set; } = string.Empty;
}
