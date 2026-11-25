using KG.MES.Main.Models;

namespace KG.MES.Main.Services;

public class LumberImportStateService
{
	private readonly Dictionary<string, KlaesOrderBatch> _orders = new(StringComparer.OrdinalIgnoreCase);

	public IReadOnlyCollection<KlaesOrderBatch> Orders => _orders.Values
		.OrderBy(o => o.OrderNumber)
		.ToList();

	public bool HasOrders => _orders.Count > 0;

	/// <summary>
	/// Добавляет или перезаписывает заказы из распарсенных строк.
	/// Возвращает списки добавленных и перезаписанных номеров заказов.
	/// </summary>
	public (List<string> added, List<string> overwritten) UpsertRows(
		IEnumerable<KlaesLumberRow> rows,
		string fileName)
	{
		var added = new List<string>();
		var overwritten = new List<string>();

		foreach (var group in rows.GroupBy(r => r.OrderNumber, StringComparer.OrdinalIgnoreCase))
		{
			var orderNumber = group.Key;
			bool exists = _orders.ContainsKey(orderNumber);

			_orders[orderNumber] = new KlaesOrderBatch
			{
				OrderNumber = orderNumber,
				FileName = fileName,
				LoadedAt = DateTime.Now,
				Rows = group.ToList()
			};

			if (exists)
				overwritten.Add(orderNumber);
			else
				added.Add(orderNumber);
		}

		return (added, overwritten);
	}

	public void RemoveOrder(string orderNumber) => _orders.Remove(orderNumber);

	public void Clear() => _orders.Clear();

	/// <summary>
	/// Сводный расчёт заготовки бруса:
	/// Сортировка: Толщина (D) DESC -> Ширина (E) DESC -> Со штапиком DESC -> Порода -> Заказ.
	/// Группировка: Толщина + Ширина + Наличие штапика + Порода (+ Номер заказа ТОЛЬКО для цельноламельной).
	/// </summary>
	public List<LumberSummaryItem> GetLumberSummary()
	{
		var lumberRows = _orders.Values
			.SelectMany(o => o.Rows)
			.Where(r => r.Section == KlaesSectionType.Lumber);

		return lumberRows
			.GroupBy(r => new
			{
				r.Thickness,
				r.Width,
				r.HasGlazingBead,
				WoodKey = r.WoodType.ToLowerInvariant(),
				OrderKey = r.IsSolidLamella ? r.OrderNumber.ToLowerInvariant() : string.Empty
			})
			.Select(g =>
			{
				var first = g.First();
				var GlazingBeadsuffix = g.Key.HasGlazingBead ? "со штапиком" : string.Empty;

				return new LumberSummaryItem
				{
					Name = $"{g.Key.Thickness}x{g.Key.Width} {GlazingBeadsuffix}",
					Thickness = g.Key.Thickness,
					Width = g.Key.Width,
					HasGlazingBead = g.Key.HasGlazingBead,
					WoodType = first.WoodType,
					IsSolidLamella = first.IsSolidLamella,
					OrderNumber = first.IsSolidLamella ? first.OrderNumber : null,
					SourceOrders = g.Select(x => x.OrderNumber)
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.OrderBy(x => x)
						.ToList(),
					TotalQuantity = g.Sum(x => x.Quantity),
					Unit = first.Unit
				};
			})
			.OrderByDescending(x => x.Thickness)
			.ThenByDescending(x => x.Width)
			.ThenByDescending(x => x.HasGlazingBead)
			.ThenBy(x => x.WoodType)
			.ThenBy(x => x.OrderNumber)
			.ToList();
	}

	/// <summary>
	/// Все позиции штапиков и горбыльков по загруженным заказам
	/// </summary>
	public List<LumberSummaryItem> GetGlazingBeadSummary()
	{
		var lumberRows = _orders.Values
			.SelectMany(o => o.Rows)
			.Where(r => r.Section == KlaesSectionType.GlazingBead);

		return lumberRows
			.GroupBy(r => new
			{
				r.Thickness,
				r.Width,
				r.HasGlazingBead,
				WoodKey = r.WoodType.ToLowerInvariant(),
				OrderKey = r.OrderNumber.ToLowerInvariant()
			})
			.Select(g =>
			{
				var first = g.First();
				return new LumberSummaryItem
				{
					Name = first.Name.TrimStart('(').TrimEnd(')'),
					Thickness = g.Key.Thickness,
					Width = g.Key.Width,
					HasGlazingBead = false,
					WoodType = first.WoodType,
					IsSolidLamella = first.IsSolidLamella,
					OrderNumber = first.OrderNumber,
					SourceOrders = g.Select(x => x.OrderNumber)
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.OrderBy(x => x)
						.ToList(),
					TotalQuantity = g.Sum(x => x.Quantity),
					Unit = first.Unit
				};
			})
			.OrderByDescending(r => r.Name)
			//.OrderByDescending(r => r.Thickness)
			//.ThenByDescending(r => r.Width)
			.ThenBy(r => r.WoodType)
			.ThenBy(r => r.OrderNumber)
			.ToList();

		//return _orders.Values
		//	.SelectMany(o => o.Rows)
		//	.Where(r => r.Section == KlaesSectionType.GlazingBead)
		//	.OrderByDescending(r => r.Thickness)   // ← сначала по толщине DESC
		//	.ThenByDescending(r => r.Width)        // ← потом по ширине DESC
		//	.ThenBy(r => r.WoodType)               // ← потом по породе
		//	.ThenBy(r => r.OrderNumber)            // ← потом по заказу
		//	.ToList();
	}

	/// <summary>
	/// Группирует сводку по сечению (Name).
	/// Данные уже отсортированы так, что одинаковые Name идут подряд.
	/// </summary>
	public List<LumberSummaryGroup> GroupSummaryItemsByName(Func<List<LumberSummaryItem>> dataProvider)
	{
		var items = dataProvider();
		var groups = new List<LumberSummaryGroup>();
		LumberSummaryGroup? currentGroup = null;

		foreach (var item in items)
		{
			if (currentGroup == null || currentGroup.SectionName != item.Name)
			{
				currentGroup = new LumberSummaryGroup
				{
					SectionName = item.Name,
					Items = [item]
				};
				groups.Add(currentGroup);
			}
			else
			{
				currentGroup.Items.Add(item);
			}
		}

		return groups;
	}

	/// <summary>
	/// Сводная группировка по брусу.
	/// </summary>
	public List<LumberSummaryGroup> GetLumberSummaryGrouped()
	{
		// Передаем ссылку на метод (без скобок!), он вызовется внутри GroupItemsByName
		return GroupSummaryItemsByName(GetLumberSummary);
	}

	/// <summary>
	/// Сводная группировка по штапикам.
	/// </summary>
	public List<LumberSummaryGroup> GetGlazingBeadSummaryGrouped()
	{
		// Передаем ссылку на метод получения штапиков
		return GroupSummaryItemsByName(GetGlazingBeadSummary);
	}
}
