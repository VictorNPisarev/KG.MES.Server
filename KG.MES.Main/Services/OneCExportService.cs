using System.Text;
using System.Text.Json;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models;
using KG.MES.Main.Models.Xml;

namespace KG.MES.Main.Services
{
	public class OneCExportService : I1CExportService
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger<OneCExportService> _logger;
		private readonly IConfiguration _configuration;

		public OneCExportService(HttpClient httpClient, ILogger<OneCExportService> logger, IConfiguration configuration)
		{
			_httpClient = httpClient;
			_logger = logger;
			_configuration = configuration;
		}

		public async Task<bool> ExportOrderAsync(XmlDocumentData order)
		{
			try
			{
				var exportData = new
				{
					OrderNumber = order.DocumentNumber,
					CustomerCode = order.CustomerNumber,
					Date = order.DocumentDate,
					TotalAmount = order.DocumentAmountNet,
					Items = order.xmlDocumentItems.Select(i => new
					{
						Article = i.ArticleNumber,
						Description = i.ShortDescription,
						Quantity = i.Quantity,
						Price = i.UnitPrice,
						Unit = i.Unit
					})
				};

				var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				});

				var apiUrl = _configuration["1CApi:BaseUrl"] ?? "http://localhost:5000/api";
				var content = new StringContent(json, Encoding.UTF8, "application/json");

				var response = await _httpClient.PostAsync($"{apiUrl}/orders", content);
				
				if (response.IsSuccessStatusCode)
				{
					_logger.LogInformation("Successfully exported order {OrderNumber} to 1C", order.DocumentNumber);
					return true;
				}
				else
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogError("Failed to export order to 1C. Status: {StatusCode}, Error: {Error}", 
						response.StatusCode, errorContent);
					return false;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error exporting order to 1C");
				return false;
			}
		}

		public async Task<string> TestConnectionAsync()
		{
			try
			{
				var apiUrl = _configuration["1CApi:BaseUrl"] ?? "http://localhost:5000/api";
				var response = await _httpClient.GetAsync($"{apiUrl}/health");
				
				return response.IsSuccessStatusCode 
					? "Connection successful" 
					: $"Connection failed: {response.StatusCode}";
			}
			catch (Exception ex)
			{
				return $"Connection error: {ex.Message}";
			}
		}
	}
}