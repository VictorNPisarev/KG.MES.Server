namespace KG.MES.Server.Models.Dto;

public class CreateOrderRequestDto
{
	public string OrderNumber { get; set; } = string.Empty;
	public DateTime? ReadyDate { get; set; }
	public int WindowCount { get; set; }
	public decimal WindowArea { get; set; }
	public int PlateCount { get; set; }
	public decimal PlateArea { get; set; }
	public bool IsEconom { get; set; }
	public bool IsClaim { get; set; }
	public bool IsOnlyPaid { get; set; }
	public string? Comment { get; set; }
	public string? Lumber { get; set; }
	public string? GlazingBead { get; set; }
	public bool IsTwoSidePaint { get; set; }
	public string? Machine { get; set; }
	public DateTime? RtmDate { get; set; }
	public DateTime? So8Date { get; set; }
	public int ApprovedLeadDays { get; set; }
	public int UnapprovedLeadDays { get; set; }
}