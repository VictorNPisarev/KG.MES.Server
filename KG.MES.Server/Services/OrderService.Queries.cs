using System.Globalization;
using KG.MES.Server.Constants;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public partial class OrderService
{
	public async Task<OrderDetailDto?> GetOrderByIdAsync(Guid orderId)
	{
		var result = await _context.Orders
			.Where(o => o.Id == orderId)
			.Join(_context.ProductionOrders, o => o.Id, po => po.OrderId, (o, po) => new { o, po })
			.Join(_context.Workplaces, x => x.po.CurrentWorkplaceId, w => w.Id, (x, w) => new OrderDetailDto
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
				CreatedAt = x.o.CreatedAt,
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

		var orders = await _context.Orders
			.Where(o => isUuid ? o.Id == orderId : o.OrderNumber == identifier)
			.ToListAsync();

		var traces = new List<OrderTraceDto>();

		foreach (var order in orders)
		{
			var productionOrder = await _context.ProductionOrders
				.FirstOrDefaultAsync(po => po.OrderId == order.Id);

			if (productionOrder == null)
			{
				continue;
			}

			var footprints = await _context.OrderFootprints
				.Where(fp => fp.ProductionOrderId == productionOrder.Id)
				.Join(_context.Workplaces, fp => fp.WorkplaceId, w => w.Id, (fp, w) => new {fp, w})
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
					Workplaces = footprints
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

			var fromWorkplaces = _context.WorkplaceTransitions
			 .Select(wt => wt.FromWorkplaceId)
			 .Distinct()
			 .ToList();

			var toWorkplaces = _context.WorkplaceTransitions
				.Select(wt => wt.ToWorkplaceId)
				.Distinct()
				.ToList();

			var allWorkplaceIds = fromWorkplaces
				.Union(toWorkplaces)
				.ToList();

			// Если граф пустой — берём все рабочие места
			if (!allWorkplaceIds.Any())
			{
				allWorkplaceIds = _context.Workplaces
					.Where(w => w.IsWorkplace)
					.Select(w => w.Id)
					.ToList();
			}

			var workplaces = _context.Workplaces
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
				Workplaces = workplaces
			};
	}
}