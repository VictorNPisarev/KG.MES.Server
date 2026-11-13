using KG.MES.Server.Extensions;
using KG.MES.Shared.Constants;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public partial class OrderService
{
	public async Task<OrderDetailDto?> GetOrderByIdAsync(Guid orderId)
	{
		var result = await context.Orders
			.Where(o => o.Id == orderId)
			.Join(context.ProductionOrders, o => o.Id, po => po.OrderId, (o, po) => new { o, po })
			.Join(context.Workplaces, x => x.po.CurrentWorkplaceId, w => w.Id, (x, w) => new OrderDetailDto
			{
				Id = x.o.Id,
				OrderNumber = x.o.OrderNumber,
				ReadyDate = x.o.ReadyDate,
				WindowCount = x.o.WindowCount,
				WindowArea = x.o.WindowArea,
				PlateCount = x.o.PlateCount,
				PlateArea = x.o.PlateArea,
				IsEconom = x.o.IsEconom,
				IsClaim = x.o.IsClaim,
				IsOnlyPaid = x.o.IsOnlyPaid,
				CreatedAt = x.o.CreatedAt.ToProductionTime(),
				ProductionOrderId = x.po.Id,
				CurrentWorkplaceId = x.po.CurrentWorkplaceId,
				CurrentStatus = w.Name,
				Comment = x.po.Comment,
				Lumber = x.po.Lumber,
				GlazingBead = x.po.GlazingBead,
				IsTwoSidePaint = x.po.IsTwoSidePaint,
				Machine = x.po.Machine,
				RtmDate = x.o.RtmDate
			})
			.FirstOrDefaultAsync();

		return result;
	}

	public async Task<OrderDetailDto?> GetOrderByNumberAsync(string orderNumber)
	{
		return await GetOrderByIdentifierQuery()
			.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
	}

	public async Task<List<OrderTraceDto>> GetOrderTraceByNumberAsync(string identifier)
	{
		var isUuid = Guid.TryParse(identifier, out var orderId);

		var orders = await context.Orders
			.Where(o => isUuid ? o.Id == orderId : o.OrderNumber == identifier)
			.ToListAsync();

		var traces = new List<OrderTraceDto>();

		foreach (var order in orders)
		{
			var productionOrder = await context.ProductionOrders
				.Include(po => po.CurrentWorkplace)
				.FirstOrDefaultAsync(po => po.OrderId == order.Id);

			if (productionOrder == null)
			{
				continue;
			}

			var footprints = await context.OrderFootprints
				.Where(fp => fp.ProductionOrderId == productionOrder.Id)
				.Join(context.Workplaces, fp => fp.WorkplaceId, w => w.Id, (fp, w) => new {fp, w})
				.OrderBy(x => x.w.Level)
				.Select(x => new WorkplaceTraceDto
				{
					WorkplaceId = x.fp.WorkplaceId,
					WorkplaceName = x.w.Name,
					Status = x.fp.Status
				})
				.ToListAsync();

			if (footprints != null && footprints.Any())
			{
				traces.Add(new OrderTraceDto
				{
					OrderId = order.Id,
					ProductionOrderId = productionOrder.Id,
					OrderNumber = order.OrderNumber,
					ReadyDate = order.ReadyDate,
					Departed = productionOrder.CurrentWorkplace?.Code == WorkplaceCodes.Departed,
					WorkplaceTraces = footprints
				});
			}
			else
			{
				traces.Add(await CreateVirtualTraces(order, productionOrder));
			}
		}

		return traces;
	}

	/// <summary>
	/// Виртуальный след для заказа, который ещё не в производстве
	/// </summary>
	private async Task<OrderTraceDto> CreateVirtualTraces(Order order, ProductionOrder productionOrder)
	{

			var fromWorkplaces = context.WorkplaceTransitions
			 .Select(wt => wt.FromWorkplaceId)
			 .Distinct()
			 .ToList();

			var toWorkplaces = context.WorkplaceTransitions
				.Select(wt => wt.ToWorkplaceId)
				.Distinct()
				.ToList();

			var allWorkplaceIds = fromWorkplaces
				.Union(toWorkplaces)
				.ToList();

			// Если граф пустой — берём все рабочие места
			if (!allWorkplaceIds.Any())
			{
				allWorkplaceIds = context.Workplaces
					.Where(w => w.IsWorkplace)
					.Select(w => w.Id)
					.ToList();
			}

			var workplaces = context.Workplaces
				.Where(w => allWorkplaceIds.Contains(w.Id) && w.IsWorkplace)
				.OrderBy(w => w.Level)
				.Select(w => new WorkplaceTraceDto
				{
					WorkplaceId = w.Id,
					WorkplaceName = w.Name,
					Status = "planned"
				})
				.ToList();

			return new OrderTraceDto
			{
				OrderId = order.Id,
				ProductionOrderId = productionOrder.Id,
				OrderNumber = order.OrderNumber,
				ReadyDate = order.ReadyDate,
				Departed = productionOrder.CurrentWorkplace?.Code == WorkplaceCodes.Departed,
				WorkplaceTraces = workplaces
			};
	}

	public async Task<List<OrderOperationLogDto>?> GetOrderOperationLogsAsync(string identifier)
	{
		// 1. Резолвим identifier → Order
		var isUuid = Guid.TryParse(identifier, out var orderId);

		var order = await context.Orders
			.FirstOrDefaultAsync(o => isUuid ? o.Id == orderId : o.OrderNumber == identifier);

		if (order == null)
			return null;

		// 2. Находим ProductionOrder (инвариант: один Order ↔ один ProductionOrder)
		var productionOrder = await context.ProductionOrders
			.FirstOrDefaultAsync(po => po.OrderId == order.Id);

		if (productionOrder == null)
			return [];

		// 3. Выбираем логи (LEFT JOIN с Users, INNER JOIN с Workplaces)
		var logs = await (
			from ol in context.OperationLogs
			join w in context.Workplaces on ol.WorkplaceId equals w.Id
			join u in context.Users on ol.UserId equals u.Id into ug
			from u in ug.DefaultIfEmpty()
			where ol.ProductionOrderId == productionOrder.Id 
				&& w.IsWorkplace
			orderby ol.OperationTime descending
			select new OrderOperationLogDto
			{
				Id = ol.Id,
				WorkplaceId = ol.WorkplaceId,
				WorkplaceName = w.Name,
				WorkplaceCode = w.Code,
				UserId = ol.UserId,
				UserName = u != null ? u.Name : null,
				OperationType = ol.OperationType,
				OperationTime = ol.OperationTime,
				Notes = ol.Notes,
				Source = ol.Source
			})
			.ToListAsync();

		// 4. Конвертация времени — в памяти, после ToListAsync (правило проекта)
		foreach (var log in logs)
			log.OperationTime = log.OperationTime.ToProductionTime();

		return logs;
	}
}