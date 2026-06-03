namespace KG.MES.Shared.Models.Dto;

public class OrderDetailDto
{
	public Guid Id { get; set; }
	public string OrderNumber { get; set; } = string.Empty;
	public DateTime? ReadyDate { get; set; }
	public int WindowCount { get; set; }
	public decimal WindowArea { get; set; }
	public int PlateCount { get; set; }
	public decimal PlateArea { get; set; }
	public bool IsEconom { get; set; }
	public bool IsClaim { get; set; }
	public bool IsOnlyPaid { get; set; }
	public DateTime CreatedAt { get; set; }
	public Guid ProductionOrderId { get; set; }
	public Guid? CurrentWorkplaceId { get; set; }
	public string? CurrentStatus { get; set; }
	public string? Comment { get; set; }
	public string? Lumber { get; set; }
	public string? GlazingBead { get; set; }
	public bool IsTwoSidePaint { get; set; }
}