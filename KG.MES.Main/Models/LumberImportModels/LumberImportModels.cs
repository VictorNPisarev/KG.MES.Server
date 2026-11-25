namespace KG.MES.Main.Models;

public enum KlaesSectionType
{
	Unknown,
	Lumber,      // Количество бруса
	GlazingBead  // Количество штапиков и горбыльков
}

public class KlaesLumberRow
{
	public string OrderNumber { get; set; } = string.Empty;
	public KlaesSectionType Section { get; set; }
	public string Name { get; set; } = string.Empty;
	public decimal Thickness { get; set; } // Столбец D
	public decimal Width { get; set; }     // Столбец E
	public string WoodType { get; set; } = string.Empty; // Столбец F
	public string Color { get; set; } = string.Empty;    // Столбец G
	public decimal Quantity { get; set; }  // Столбец H
	public string Unit { get; set; } = "м"; // Столбец I

	/// <summary>
	/// Возможность отделения штапика (только для бруса)
	/// </summary>
	public bool HasGlazingBead =>
		Section == KlaesSectionType.Lumber &&
		Name.Contains("со штапиком", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Цельноламельная древесина (не объединяется между разными заказами)
	/// </summary>
	public bool IsSolidLamella =>
		WoodType.Contains("цельн", StringComparison.OrdinalIgnoreCase);
}

public class KlaesOrderBatch
{
	public string OrderNumber { get; set; } = string.Empty;
	public string FileName { get; set; } = string.Empty;
	public DateTime LoadedAt { get; set; } = DateTime.Now;
	public List<KlaesLumberRow> Rows { get; set; } = [];

	public decimal TotalLumberMeters => Rows
		.Where(r => r.Section == KlaesSectionType.Lumber)
		.Sum(r => r.Quantity);

	public decimal TotalBeadMeters => Rows
		.Where(r => r.Section == KlaesSectionType.GlazingBead)
		.Sum(r => r.Quantity);
}

public class LumberSummaryItem
{
	public string Name { get; set; } = string.Empty;
	public decimal Thickness { get; set; }
	public decimal Width { get; set; }
	public bool HasGlazingBead { get; set; }
	public string WoodType { get; set; } = string.Empty;
	public bool IsSolidLamella { get; set; }

	/// <summary>
	/// Заполняется только для цельноламельного бруса; для сращённого/тангенса = null
	/// </summary>
	public string? OrderNumber { get; set; }

	/// <summary>
	/// Список всех заказов, вошедших в эту строку сводки
	/// </summary>
	public List<string> SourceOrders { get; set; } = new();

	public decimal TotalQuantity { get; set; }
	public string Unit { get; set; } = "м";
}
