using System.Globalization;
using System.Text;
using ExcelDataReader;
using KG.MES.Main.Models;

namespace KG.MES.Main.Services;

public class KlaesXlsParserService
{
	public List<KlaesLumberRow> Parse(Stream stream, string fileName)
	{
		using var ms = new MemoryStream();
		stream.CopyTo(ms);
		ms.Position = 0;

		List<List<string>> grid;
		try
		{
			grid = ReadViaExcelDataReader(ms);
		}
		catch
		{
			ms.Position = 0;
			grid = ReadViaDelimitedText(ms);
		}

		return ParseGrid(grid, fileName);
	}

	private static List<List<string>> ReadViaExcelDataReader(Stream stream)
	{
		var rows = new List<List<string>>();
		using var reader = ExcelReaderFactory.CreateReader(stream);

		while (reader.Read())
		{
			var row = new List<string>(reader.FieldCount);
			for (int i = 0; i < reader.FieldCount; i++)
			{
				var val = reader.GetValue(i);
				row.Add(Convert.ToString(val, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty);
			}
			rows.Add(row);
		}

		return rows;
	}

	private static List<List<string>> ReadViaDelimitedText(Stream stream)
	{
		var bytes = ((MemoryStream)stream).ToArray();
		var text = Encoding.UTF8.GetString(bytes);
		if (text.Contains('\uFFFD'))
		{
			text = Encoding.GetEncoding(1251).GetString(bytes);
		}

		var firstLine = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
		char delimiter = firstLine.Count(c => c == ';') >= firstLine.Count(c => c == ',')
			? (firstLine.Contains(';') ? ';' : '\t')
			: ',';

		var rows = new List<List<string>>();
		var currentRow = new List<string>();
		var field = new StringBuilder();
		bool inQuotes = false;

		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];

			if (c == '"')
			{
				if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
				{
					field.Append('"');
					i++;
				}
				else
				{
					inQuotes = !inQuotes;
				}
			}
			else if (c == delimiter && !inQuotes)
			{
				currentRow.Add(field.ToString().Trim());
				field.Clear();
			}
			else if ((c == '\r' || c == '\n') && !inQuotes)
			{
				if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
					i++;

				currentRow.Add(field.ToString().Trim());
				field.Clear();
				rows.Add(currentRow);
				currentRow = new List<string>();
			}
			else
			{
				field.Append(c);
			}
		}

		if (field.Length > 0 || currentRow.Count > 0)
		{
			currentRow.Add(field.ToString().Trim());
			rows.Add(currentRow);
		}

		return rows;
	}

	private static List<KlaesLumberRow> ParseGrid(List<List<string>> grid, string fileName)
	{
		var result = new List<KlaesLumberRow>();
		var currentSection = KlaesSectionType.Unknown;

		foreach (var rawRow in grid)
		{
			if (rawRow.All(string.IsNullOrWhiteSpace))
				continue;

			// Смещение: в выгрузке Klaes столбец A пустой, данные начинаются со столбца B (индекс 1),
			// но поддерживаем и случай без пустой первой колонки
			int offset = string.IsNullOrWhiteSpace(rawRow[0]) && rawRow.Count > 1 ? 1 : 0;
			var colB = GetCell(rawRow, offset);

			if (colB.Equals("Количество бруса", StringComparison.OrdinalIgnoreCase))
			{
				currentSection = KlaesSectionType.Lumber;
				continue;
			}

			if (colB.StartsWith("Количество штапиков", StringComparison.OrdinalIgnoreCase))
			{
				currentSection = KlaesSectionType.GlazingBead;
				continue;
			}

			if (currentSection == KlaesSectionType.Unknown)
				continue;

			var orderNumber = colB;
			var name = NormalizeMultiline(GetCell(rawRow, offset + 1));
			var thicknessStr = GetCell(rawRow, offset + 2);
			var widthStr = GetCell(rawRow, offset + 3);
			var woodType = NormalizeMultiline(GetCell(rawRow, offset + 4));
			var color = NormalizeMultiline(GetCell(rawRow, offset + 5));
			var qtyStr = GetCell(rawRow, offset + 6);
			var unit = GetCell(rawRow, offset + 7);

			if (string.IsNullOrWhiteSpace(orderNumber) ||
				!TryParseDecimal(thicknessStr, out var thickness) ||
				!TryParseDecimal(widthStr, out var width) ||
				!TryParseDecimal(qtyStr, out var qty))
			{
				continue;
			}

			result.Add(new KlaesLumberRow
			{
				OrderNumber = orderNumber.Trim(),
				Section = currentSection,
				Name = name,
				Thickness = thickness,
				Width = width,
				WoodType = woodType,
				Color = color,
				Quantity = qty,
				Unit = string.IsNullOrWhiteSpace(unit) ? "м" : unit
			});
		}

		if (result.Count == 0)
			throw new InvalidOperationException($"В файле «{fileName}» не найдены строки бруса или штапиков.");

		return result;
	}

	private static string GetCell(List<string> row, int index) =>
		index >= 0 && index < row.Count ? row[index] : string.Empty;

	private static string NormalizeMultiline(string value) =>
		string.Join(" ", value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
			.Trim();

	private static bool TryParseDecimal(string input, out decimal value)
	{
		var normalized = input.Replace(" ", "").Replace(',', '.');
		return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
	}
}
