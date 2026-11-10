using System.Text.Json;
using KG.MES.Main.Interfaces;
using KG.MES.Main.Models;
using KG.MES.Main.Models.Enums;

namespace KG.MES.Main.Services
{
	public class MaterialTypeConfigService
	{
		private MaterialTypeConfig _config = new();
		private Dictionary<XmlBlockType, List<MaterialTypeRule>> _rulesBySource = new();
		private Dictionary<XmlBlockType, string> _defaultTypeBySource = new();
		
		public async Task LoadAsync(string configPath)
		{
			var json = await File.ReadAllTextAsync(configPath);
			_config = JsonSerializer.Deserialize<MaterialTypeConfig>(json) ?? new();
			
			// Группируем правила по XmlBlockType
			foreach (var rule in _config.Rules)
			{
				if (!Enum.TryParse<XmlBlockType>(rule.XmlSource, out var blockType))
					continue;
				
				if (!_rulesBySource.ContainsKey(blockType))
					_rulesBySource[blockType] = new List<MaterialTypeRule>();
				
				_rulesBySource[blockType].Add(rule);
				
				// Правило без ключевых слов = тип по умолчанию для этого источника
				if (!rule.ArticleKeywords.Any() && !rule.DescriptionKeywords.Any())
				{
					_defaultTypeBySource[blockType] = rule.Type;
				}
			}
			
			// Сортируем правила по приоритету
			foreach (var source in _rulesBySource.Keys)
			{
				_rulesBySource[source] = _rulesBySource[source]
					.OrderByDescending(r => r.Priority)
					.ToList();
			}
		}
		
		/// <summary>
		/// Универсальный метод определения типа материала
		/// </summary>
		public (string type, string? unit, double coefficient, bool skip) ResolveType(IXmlDataMaterial xmlDataMaterial)
		{
			var blockType = xmlDataMaterial.XmlBlockType;
			
			// Если для этого блока нет правил
			if (!_rulesBySource.ContainsKey(blockType))
				return ("Other", null, 1.0, false);
			
			var text = $"{xmlDataMaterial.ArticleNumber} {xmlDataMaterial.Name}".ToLowerInvariant();
			
			foreach (var rule in _rulesBySource[blockType])
			{
				// Правило без ключевых слов - подходит для любого материала этого блока
				if (!rule.ArticleKeywords.Any() && !rule.DescriptionKeywords.Any())
				{
					return (rule.Type, rule.Unit, rule.Coefficient, rule.Skip);
				}
				
				// Проверка по ключевым словам в артикуле
				if (rule.ArticleKeywords.Any() && 
					rule.ArticleKeywords.Any(k => text.Contains(k.ToLowerInvariant())))
				{
					return (rule.Type, rule.Unit, rule.Coefficient, rule.Skip);
				}
				
				// Проверка по ключевым словам в описании
				if (rule.DescriptionKeywords.Any() && 
					rule.DescriptionKeywords.Any(k => text.Contains(k.ToLowerInvariant())))
				{
					return (rule.Type, rule.Unit, rule.Coefficient, rule.Skip);
				}
			}
			
			// Если ничего не подошло, но есть тип по умолчанию
			if (_defaultTypeBySource.TryGetValue(blockType, out var defaultType))
				return (defaultType, null, 1.0, false);
			
			return ("Other", null, 1.0, false);
		}
		
		public string GetUnitForMaterial(string typeKey, string? ruleUnit)
		{
			if (!string.IsNullOrEmpty(ruleUnit))
				return ruleUnit;
			
			return _config.Types.GetValueOrDefault(typeKey)?.DefaultUnit ?? "шт";
		}
		
		public Models.MaterialType GetType(string key)
		{
			return _config.Types.GetValueOrDefault(key) ?? new MaterialType();
		}
	}
}