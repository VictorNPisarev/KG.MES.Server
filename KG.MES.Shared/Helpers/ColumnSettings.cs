using System.Text.Json.Serialization;

namespace KG.MES.Shared.Helpers;

public class ColumnSetting
{
	[JsonPropertyName("propertyName")]
	public string PropertyName { get; set; } = string.Empty;

	[JsonPropertyName("visible")]
	public bool Visible { get; set; } = true;

	[JsonPropertyName("order")]
	public int Order { get; set; }

	[JsonPropertyName("width")]
	public int Width { get; set; }
}
