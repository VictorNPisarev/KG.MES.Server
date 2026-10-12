// Models/MaterialConfig.cs
using System.Text.Json.Serialization;

namespace KG.MES.Main.Models
{
	public class MaterialTypeRule
	{
		[JsonPropertyName("type")]
		public string Type { get; set; } = string.Empty;  // "Packaging"
		
		[JsonPropertyName("xmlSource")]
		public string XmlSource { get; set; } = string.Empty;
		
		[JsonPropertyName("priority")]
		public int Priority { get; set; }
		
		[JsonPropertyName("unit")]
		public string? Unit { get; set; }  // Если указана - переопределяет defaultUnit из типа
		
		[JsonPropertyName("articleKeywords")]
		public List<string> ArticleKeywords { get; set; } = new();
		
		[JsonPropertyName("descriptionKeywords")]
		public List<string> DescriptionKeywords { get; set; } = new();

		[JsonPropertyName("coefficient")]
		public double Coefficient { get; set; } = 1.0;

		[JsonPropertyName("skip")]
		public bool Skip { get; set; } = false;  // true - не добавлять в материалы

	}
}