using System.Xml;
using System.Xml.Serialization;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models;
using KG.MES.Main.Models.Xml;

namespace KG.MES.Main.Services
{
	public class XmlReaderService : IXmlReaderService
	{
		private readonly ILogger<XmlReaderService> _logger;

		public XmlReaderService(ILogger<XmlReaderService> logger)
		{
			_logger = logger;
		}

		public async Task<XmlDocumentData> ReadXmlFromContent(string xmlContent)
		{
			// Устанавливаем инвариантную культуру (с точкой)
			var culture = System.Globalization.CultureInfo.InvariantCulture;
			System.Threading.Thread.CurrentThread.CurrentCulture = culture;
			System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
 
			return await Task.Run(() =>
			{
				try
				{
					_logger.LogInformation("Starting XML deserialization");
					
					// Очищаем XML от возможных проблемных символов
					xmlContent = CleanXmlContent(xmlContent);
					
					var serializer = new XmlSerializer(typeof(XmlDocumentData));
					
					using var reader = new StringReader(xmlContent);
					var xmlDocumentData = (XmlDocumentData?)serializer.Deserialize(reader);
					
					if (xmlDocumentData == null)
						throw new InvalidOperationException("Failed to deserialize XML - result is null");
					
					// Валидация данных
					xmlDocumentData.Validate();
					
					_logger.LogInformation("Successfully read XML document: {DocumentNumber} with {ItemCount} items", 
						xmlDocumentData.DocumentNumber, xmlDocumentData.xmlDocumentItems.Count);
					
					// Логируем дополнительную информацию
					LogDocumentInfo(xmlDocumentData);
					
					return xmlDocumentData;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error reading XML content");
					throw new XmlException($"XML parsing error: {ex.Message}", ex);
				}
			});
		}

		public async Task<XmlDocumentData> ReadXmlFromFile(string filePath)
		{
			if (!File.Exists(filePath))
				throw new FileNotFoundException($"XML file not found: {filePath}");

			_logger.LogInformation("Reading XML file: {FilePath}", filePath);
			
			var xmlContent = await File.ReadAllTextAsync(filePath);
			return await ReadXmlFromContent(xmlContent);
		}

		private string CleanXmlContent(string xmlContent)
		{
			// Удаляем BOM если есть
			if (xmlContent.StartsWith("\uFEFF", StringComparison.Ordinal))
				xmlContent = xmlContent[1..];

			// Заменяем проблемные символы
			xmlContent = xmlContent.Replace("&#x0;", "")
								  .Replace("\0", "")
								  .Trim();

			return xmlContent;
		}

		private void LogDocumentInfo(XmlDocumentData xmlDocumentData)
		{
			_logger.LogInformation("Document Info - Number: {Number}, Customer: {Customer}, Date: {Date}, Net: {Net}, Gross: {Gross}",
				xmlDocumentData.DocumentNumber,
				xmlDocumentData.CustomerNumber,
				xmlDocumentData.DocumentDate,
				xmlDocumentData.DocumentAmountNet,
				xmlDocumentData.DocumentAmountGross);

			foreach (var item in xmlDocumentData.xmlDocumentItems)
			{
				_logger.LogDebug("Item {Number}: {Description}, Qty: {Quantity}, Price: {Price}",
					item.ItemNumber, item.ShortDescription, item.Quantity, item.UnitPrice);
			}
		}
	}
}