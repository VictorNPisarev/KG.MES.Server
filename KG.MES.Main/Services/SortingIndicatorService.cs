// Services/SortingIndicatorService.cs
using System.Text.Json;
using KG.MES.Main.Models;
using KG.MES.Main.Models.Enums;

namespace KG.MES.Main.Services
{
	public class SortingIndicatorService
	{
		private readonly ILogger<SortingIndicatorService> _logger;
		private List<SortingIndicator> _indicators = new();
		private List<IndicatorCategory> _categories = new();
		private Dictionary<string, SortingIndicator> _indicatorMap = new();
		private Dictionary<string, IndicatorCategory> _categoryMap = new();
		private Dictionary<IndicatorCategoryType, List<string>> _categoryIndicators = new();

		public SortingIndicatorService(ILogger<SortingIndicatorService> logger)
		{
			_logger = logger;
		}

		public async Task LoadAsync(string indicatorsPath, string categoriesPath)
		{
			try
			{
				// Загружаем индикаторы
				var indicatorsJson = await File.ReadAllTextAsync(indicatorsPath);
				var indicatorsData = JsonSerializer.Deserialize<IndicatorsRoot>(indicatorsJson);
				_indicators = indicatorsData?.Indicators ?? new();

				// Загружаем категории (маппинг индикаторов к категориям)
				var categoriesJson = await File.ReadAllTextAsync(categoriesPath);
				var categoriesData = JsonSerializer.Deserialize<CategoriesRoot>(categoriesJson);
				
				// Инициализируем словарь
				foreach (IndicatorCategoryType category in Enum.GetValues(typeof(IndicatorCategoryType)))
				{
					_categoryIndicators[category] = new List<string>();
				}
				
				// Заполняем из JSON
				if (categoriesData?.Categories != null)
				{
					foreach (var categoryJson in categoriesData.Categories)
					{
						if (Enum.TryParse<IndicatorCategoryType>(categoryJson.CategoryKey, true, out var category))
						{
							_categoryIndicators[category] = categoryJson.Indicators;
						}
					}
				}
				
				_logger.LogInformation("Loaded {IndicatorCount} indicators and {CategoryCount} categories", 
					_indicators.Count, _categoryIndicators.Count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load indicators or categories");
			}
		}
		
		public SortingIndicator? GetIndicator(string code) => 
			string.IsNullOrEmpty(code) ? null : _indicatorMap.GetValueOrDefault(code);
		
		public string GetIndicatorName(string code) => 
			GetIndicator(code)?.Name ?? code;
			
		public IndicatorCategory? GetCategory(string key)
		{
			return _categoryMap.GetValueOrDefault(key);
		}
		
		public List<IndicatorCategoryType> GetCategoriesForIndicator(string code)
		{
			var categories = new List<IndicatorCategoryType>();
			foreach (var kvp in _categoryIndicators)
			{
				if (kvp.Value.Contains(code))
					categories.Add(kvp.Key);
			}
			return categories;
		}
		
		public List<string> GetIndicatorsForCategory(IndicatorCategoryType category) =>
			_categoryIndicators.GetValueOrDefault(category) ?? new();
		
		public bool IsInCategory(string code, IndicatorCategoryType category)
		{
			if (string.IsNullOrEmpty(code)) return false;
			return _categoryIndicators.GetValueOrDefault(category)?.Contains(code) ?? false;
		}
		
		public List<IndicatorCategory> GetAllCategories() => _categories;
		
		public List<SortingIndicator> GetAllIndicators() => _indicators;
	}
}