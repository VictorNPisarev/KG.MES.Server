// Models/MaterialConfig.cs
using System.Text.Json.Serialization;

namespace KG.MES.Main.Models
{
	public class MaterialType
	{
		[JsonPropertyName("key")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string Description { get; set; } = string.Empty;
		
		[JsonPropertyName("icon")]
		public string? Icon { get; set; }
		
		[JsonPropertyName("color")]
		public string? Color { get; set; }
		
		[JsonPropertyName("defaultUnit")]
		public string? DefaultUnit { get; set; }
	}
}