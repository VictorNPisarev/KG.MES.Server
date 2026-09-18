using KG.MES.Main.Models;
using KG.MES.Shared.Models.Dto;
using KG.MES.Main.Models.Enums;
using KG.MES.Main.Services;

namespace KG.MES.Main.Extensions
{
	public static class OrderMappingExtensions
	{

		public static ProductionOrderExportDto ToProductionOrderDto(this DocumentData order,
			SortingIndicatorService sortingService)
		{
			// Подсчет окон (конструкции + стеклопакеты)
			var windowItems = order.DocumentItems
				.Where(i => i.ItemType == DocumentItemType.Konstruktion ||
						   i.ItemType == DocumentItemType.GlassProduct)
				.ToList();

			var windowCount = windowItems.Sum(i => (int)i.Piece);
			var windowArea = (double)windowItems.Sum(i => (double)(i.Area * i.Piece));

			// Подсчет щитовых изделий (подоконники, откосы и т.д.)
			var plateItems = order.DocumentItems
				.Where(i => i.IsInCategory(IndicatorCategoryType.PanelProducts, sortingService) ||
						   i.IsInCategory(IndicatorCategoryType.WindowSills, sortingService) ||
						   i.IsInCategory(IndicatorCategoryType.Slopes, sortingService))
				.ToList();

			var plateCount = plateItems.Sum(i => (int)i.Piece);
			var plateArea = (double)plateItems.Sum(i => (double)(i.Area * i.Piece));

			// Определение породы дерева (берем из первой позиции-конструкции)
			var lumber = order.DocumentItems
				.FirstOrDefault(i => i.ItemType == DocumentItemType.Konstruktion)
				?.WoodType ?? "";

			// Штапик (из материалов или описания)
			var glazingBead = ExtractGlazingBead(order);

			// Двусторонняя покраска (из XML, readonly)
			var isTwoSidePaint = order.IsDoubleColor();

			// Комментарий (по умолчанию из XML, но пользователь может изменить)
			var defaultComment = BuildDefaultComment(order);

			return new ProductionOrderExportDto
			{
				OrderNumber = order.DocumentNumber ?? "",
				WindowCount = windowCount,
				WindowArea = windowArea,
				PlateCount = plateCount,
				PlateArea = plateArea,
				Lumber = lumber,
				GlazingBead = glazingBead,
				IsTwoSidePaint = isTwoSidePaint,

				// Редактируемые поля — значения по умолчанию
				IsEconom = order.IsEconom,
				IsClaim = order.IsClaim,
				IsOnlyPaid = order.IsOnlyPaid,
				Comment = order.Comment,
				RtmDate = order.InitializeDate,
				ApprovedLeadDays = order.ApprovedLeadTimeDays,
				ReadyDate = order.ReadyDate
			};
		}

		private static string? ExtractGlazingBead(DocumentData order)
		{
			// Ищем в материалах штапик
			var glazingBeadMaterial = order.DocumentItems
				.SelectMany(i => i.Materials)
				.FirstOrDefault(m => m.Name?.Contains("штапик", StringComparison.OrdinalIgnoreCase) == true);

			if (glazingBeadMaterial != null)
				return glazingBeadMaterial.Name;

			// Или парсим из описания
			var konstruktion = order.DocumentItems
				.FirstOrDefault(i => i.ItemType == DocumentItemType.Konstruktion);

			if (konstruktion?.Description != null)
			{
				var match = System.Text.RegularExpressions.Regex.Match(
					konstruktion.Description,
					@"штапик\s*([^,|]+)",
					System.Text.RegularExpressions.RegexOptions.IgnoreCase);
				if (match.Success)
					return match.Groups[1].Value.Trim();
			}

			return "Штапик 22/19.5 мм"; // значение по умолчанию
		}

		private static string? BuildDefaultComment(DocumentData order)
		{
			var parts = new List<string>();

			if (!string.IsNullOrEmpty(order.ProjectNumber))
				parts.Add($"Проект: {order.ProjectNumber}");

			if (!string.IsNullOrEmpty(order.ProjectDescription))
				parts.Add(order.ProjectDescription);

			if (!string.IsNullOrEmpty(order.Description))
				parts.Add(order.Description);

			return parts.Count > 0 ? string.Join(" | ", parts) : null;
		}
	}
}
