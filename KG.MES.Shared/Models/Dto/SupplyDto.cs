using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Dto;

public class SupplyDto : OrderDtoBase
{
	[JsonPropertyName("lumber")]
	public string? Lumber { get; set; }

	[JsonPropertyName("lumber_comment")]
	public string? LumberComment { get; set; }

	[JsonPropertyName("paint")]
	public string? Paint { get; set; }

	[JsonPropertyName("paint_comment")]
	public string? PaintComment { get; set; }

	[JsonPropertyName("glass")]
	public string? Glass { get; set; }

	[JsonPropertyName("glass_comment")]
	public string? GlassComment { get; set; }

	[JsonPropertyName("furniture")]
	public string? Furniture { get; set; }

	[JsonPropertyName("furniture_comment")]
	public string? FurnitureComment { get; set; }

	[JsonPropertyName("alumWaterShield")]
	public string? AlumWaterShield { get; set; }

	[JsonPropertyName("alumWaterShield_comment")]
	public string? AlumWaterShieldComment { get; set; }

	[JsonPropertyName("windowsill")]
	public string? Windowsill { get; set; }

	[JsonPropertyName("windowsill_comment")]
	public string? WindowsillComment { get; set; }

	[JsonPropertyName("woodAlum")]
	public string? WoodAlum { get; set; }

	[JsonPropertyName("woodAlum_comment")]
	public string? WoodAlumComment { get; set; }
}