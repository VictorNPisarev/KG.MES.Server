// KG.MES.Shared/Serialization/TotalsDtoConverter.cs
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KG.MES.Shared.Common.Attributes;
using KG.MES.Shared.Models.Dto;

namespace KG.MES.Shared.Serialization;

/// <summary>
/// Конвертер для полиморфной десериализации ITotalsDto.
/// Тип определяется по атрибуту [TotalsType("...")].
/// </summary>
public class TotalsDtoConverter : JsonConverter<ITotalsDto>
{
	private const string DiscriminatorProperty = "totalsType";

	private static readonly Dictionary<string, Type> typeMap = BuildTypeMap();

	public override ITotalsDto? Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;

		// Читаем дискриминатор
		if (!root.TryGetProperty(DiscriminatorProperty, out var typeElement))
			return null;

		var totalsType = typeElement.GetString();
		if (string.IsNullOrEmpty(totalsType))
			return null;

		// Ищем тип в словаре
		if (!typeMap.TryGetValue(totalsType, out var targetType))
			return null;

		// Десериализуем в конкретный тип
		return (ITotalsDto?)JsonSerializer.Deserialize(
			root.GetRawText(),
			targetType,
			options);
	}

	public override void Write(
		Utf8JsonWriter writer,
		ITotalsDto value,
		JsonSerializerOptions options)
	{
		// При сериализации добавляем дискриминатор
		var actualType = value.GetType();
		var attribute = actualType.GetCustomAttribute<TotalsTypeAttribute>();

		if (attribute == null)
		{
			// Без атрибута — просто сериализуем как есть
			JsonSerializer.Serialize(writer, value, actualType, options);
			return;
		}

		// Сериализуем объект
		using var doc = JsonDocument.Parse(
			JsonSerializer.Serialize(value, actualType, options));

		writer.WriteStartObject();

		// Добавляем дискриминатор первым
		writer.WriteString(DiscriminatorProperty, attribute.Name);

		// Добавляем остальные поля
		foreach (var property in doc.RootElement.EnumerateObject())
		{
			property.WriteTo(writer);
		}

		writer.WriteEndObject();
	}

	/// <summary>
	/// Строит карту типов на основе атрибутов [TotalsType] в текущей сборке
	/// </summary>
	private static Dictionary<string, Type> BuildTypeMap()
	{
		var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		var assembly = typeof(ITotalsDto).Assembly;

		foreach (var type in assembly.GetTypes())
		{
			// Пропускаем интерфейсы, абстрактные классы, не-ITotalsDto
			if (!typeof(ITotalsDto).IsAssignableFrom(type)
				|| type.IsInterface
				|| type.IsAbstract)
				continue;

			var attribute = type.GetCustomAttribute<TotalsTypeAttribute>();
			if (attribute == null)
				continue;

			if (map.ContainsKey(attribute.Name))
				throw new InvalidOperationException(
					$"Duplicate TotalsType: '{attribute.Name}' used by {map[attribute.Name].Name} and {type.Name}");

			map[attribute.Name] = type;
		}

		return map;
	}
}