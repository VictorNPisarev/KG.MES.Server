using System.Text.Json.Serialization;
using KG.MES.Shared.Common.Attributes;

namespace KG.MES.Shared.Models.Dto;

/// <summary>
/// Итоговые суммы по заказам (по всей выборке, а не по странице)
/// </summary>
[TotalsType("order")]
public class OrderTotalsDto : ITotalsDto
{
	[JsonPropertyName("window_count_total")]
	public int? WindowCountTotal { get; set; }

	[JsonPropertyName("window_area_total")]
	public decimal? WindowAreaTotal { get; set; }

	[JsonPropertyName("plate_count_total")]
	public int? PlateCountTotal { get; set; }

	[JsonPropertyName("plate_area_total")]
	public decimal? PlateAreaTotal { get; set; }
}