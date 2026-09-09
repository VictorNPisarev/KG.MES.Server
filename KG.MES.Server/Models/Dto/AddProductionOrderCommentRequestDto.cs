namespace KG.MES.Shared.Models.Dto;


public class AddProductionOrderCommentRequestDto
{
	public Guid ProductionOrderId { get; set; }
	public Guid? UserId { get; set; }
	public string Content { get; set; } = string.Empty;
}
