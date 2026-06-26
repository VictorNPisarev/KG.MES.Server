using KG.MES.Server.Data;
using KG.MES.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public class OrderAttributeService(AppDbContext context, ILogger<OrderAttributeService> logger)
{
	private readonly AppDbContext _context = context;
	private readonly ILogger<OrderAttributeService> _logger = logger;
	
	public async Task<Dictionary<Guid, List<OrderAttributeDto>>> GetAttributesForWorkplace(
		Guid workplaceId,
		List<OrderWorkplaceDto> orderWorkplaceDtos)
	{
		var result = new Dictionary<Guid, List<OrderAttributeDto>>();

		if (orderWorkplaceDtos.Count == 0)
			return result;

		var productionOrderIds = orderWorkplaceDtos.Select(f => f.ProductionOrderId).ToList();

		var productionOrders = await _context.ProductionOrders
			.Where(po => productionOrderIds.Contains(po.Id))
			.ToListAsync();

		var orders = await _context.Orders
			.Where(o => productionOrders.Select(p => p.OrderId).Contains(o.Id))
			.ToListAsync();

		// Сначала получаем ID заказов
		var orderIds = productionOrders.Select(p => p.OrderId).ToList();

		// Потом получаем ID OrderSupply для этих заказов
		var orderSupplyIds = await _context.OrderSupplies
			.Where(os => orderIds.Contains(os.OrderId))
			.Select(os => os.Id)
			.ToListAsync();

		//// Теперь загружаем SupplyItems с правильным порядком Include/Where
		//var supplyItems = await _context.SupplyItems
		//	.Include(si => si.OrderSupply)
		//	.Include(si => si.SupplyType)
		//	.Include(si => si.Condition)
		//	.Where(si => orderSupplyIds.Contains(si.OrderSupplyId))
		//	.ToListAsync();
			
		var footprintsAll = await _context.OrderFootprints
			.Where(fp => productionOrderIds.Contains(fp.ProductionOrderId))
			.ToListAsync();

		var workplaceIds = await _context.Workplaces
			.Where(w => w.IsWorkplace)
			.ToDictionaryAsync(w => w.Name, w => w.Id);

		foreach (var footprint in orderWorkplaceDtos)
		{
			var attributes = new List<OrderAttributeDto>();
			var productionOrder = productionOrders.FirstOrDefault(po => po.Id == footprint.ProductionOrderId);
			if (productionOrder == null) continue;

			var order = orders.FirstOrDefault(o => o.Id == productionOrder.OrderId);
			if (order == null) continue;

			// === АТРИБУТЫ ПО УЧАСТКАМ ===

			//// 1. Для торцовки и столярки (материалы)
			//if (workplaceId == workplaceIds.GetValueOrDefault("Торцовка") ||
			//	workplaceId == workplaceIds.GetValueOrDefault("Столярка"))
			//{
			//	var lumber = supplyItems.FirstOrDefault(s =>
			//		s.OrderSupply?.OrderId == order.Id &&
			//		s.SupplyType?.Name == "lumber");

			//	if (lumber != null)
			//	{
			//		if (lumber.Condition?.ConditionCode == "in_stock")
			//		{
			//			attributes.Add(new OrderAttributeDto
			//			{
			//				Key = "lumber",
			//				Icon = "✅",
			//				DisplayText = "Брус в наличии",
			//				Value = "in_stock"
			//			});
			//		}
			//		else if (lumber.Condition?.ConditionCode == "ordered")
			//		{
			//			attributes.Add(new OrderAttributeDto
			//			{
			//				Key = "lumber",
			//				Icon = "🚛",
			//				DisplayText = "Брус заказан",
			//				Value = "ordered"
			//			});
			//		}
			//	}
			//}

			//// 2. Для фурнитуры
			//if (workplaceId == workplaceIds.GetValueOrDefault("Фурнитура"))
			//{
			//	var furniture = supplyItems.FirstOrDefault(s =>
			//		s.OrderSupply?.OrderId == order.Id &&
			//		s.SupplyType?.Name == "furniture" &&
			//		s.Condition?.ConditionCode == "in_stock");

			//	if (furniture != null)
			//	{
			//		attributes.Add(new OrderAttributeDto
			//		{
			//			Key = "furniture",
			//			Icon = "🔑",
			//			DisplayText = "Фурнитура в наличии",
			//			Value = true
			//		});
			//	}
			//}

			//// 3. Для остекления
			//if (workplaceId == workplaceIds.GetValueOrDefault("Остекление"))
			//{
			//	var glass = supplyItems.FirstOrDefault(s =>
			//		s.OrderSupply?.OrderId == order.Id &&
			//		s.SupplyType?.Name == "glass" &&
			//		s.Condition?.ConditionCode == "in_stock");

			//	if (glass != null)
			//	{
			//		attributes.Add(new OrderAttributeDto
			//		{
			//			Key = "glass",
			//			Icon = "✅",
			//			DisplayText = "Стеклопакеты в наличии",
			//			Value = true
			//		});
			//	}
			//}

			// 4. Для профилирования (станок)
			if (workplaceId == workplaceIds.GetValueOrDefault("Профилирование"))
			{
				var machine = productionOrder.Machine; // нужно добавить поле Machine в Order
				if (!string.IsNullOrEmpty(machine))
				{
					var icon = machine == "Conturex" ? "📟" : "📐";
					attributes.Add(new OrderAttributeDto
					{
						Key = "machine",
						Icon = icon,
						DisplayText = machine,
						Value = true
					});
				}
			}

			// 5. Для упаковки (проверка предыдущих участков)
			if (workplaceId == workplaceIds.GetValueOrDefault("Упаковка"))
			{
				var orderFootprints = footprintsAll.Where(fp => fp.ProductionOrderId == footprint.ProductionOrderId).ToList();
				var glazingId = workplaceIds.GetValueOrDefault("Остекление");
				var furnitureId = workplaceIds.GetValueOrDefault("Фурнитура");

				var glazingCompleted = orderFootprints.Any(fp =>
					fp.WorkplaceId == glazingId && fp.Status == "completed");
				var furnitureCompleted = orderFootprints.Any(fp =>
					fp.WorkplaceId == furnitureId && fp.Status == "completed");

				if (glazingCompleted && furnitureCompleted)
				{
					attributes.Add(new OrderAttributeDto
					{
						Key = "previous_ready",
						Icon = "✅",
						DisplayText = "Предыдущие участки завершены",
						Value = true
					});
				}
			}

			result[footprint.ProductionOrderId] = attributes;
		}

		return result;
	}
}