namespace KG.MES.Shared.Models.Dto;

public class ProductionOrderExportDto
{
	// Автоматически из XML
	public string OrderNumber { get; set; } = string.Empty;
	public int WindowCount { get; set; }
	public double WindowArea { get; set; }
	public int PlateCount { get; set; }
	public double PlateArea { get; set; }
	public string? Lumber { get; set; }
	public string? GlazingBead { get; set; }
	public bool IsTwoSidePaint { get; set; }

	// Редактируемые поля
	public bool IsEconom { get; set; } = false;
	public bool IsClaim { get; set; } = false;
	public bool IsOnlyPaid { get; set; } = false;
	public string? Comment { get; set; }
	public DateTime RtmDate { get; set; } = DateTime.Now;  // "2026-04-21"
	public int ApprovedLeadDays { get; set; } = 60;          // срок изготовления в днях СОГЛАСОВАННЫй
	public int UnapprovedLeadDays { get; set; } = 60;          // срок изготовления в днях НЕ СОГЛАСОВАННЫй
	public DateTime? ReadyDate { get; set; }  // вычисляется на сервере или клиенте
	public DateTime? So8Date { get; set; }  //дата запуска заказа в СО8
	public string? Machine { get; set; }  //Станок профилирования
}
