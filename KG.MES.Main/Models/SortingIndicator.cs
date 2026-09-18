// Models/SortingIndicator.cs
using System.Text.Json.Serialization;

namespace KG.MES.Main.Models
{
	public class SortingIndicator
	{
		[JsonPropertyName("code")]
		public string Code { get; set; } = string.Empty;
		
		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;
		
		[JsonPropertyName("description")]
		public string? Description { get; set; }
	}

	public class IndicatorsRoot
	{
		[JsonPropertyName("indicators")]
		public List<SortingIndicator> Indicators { get; set; } = new();
	}
}
