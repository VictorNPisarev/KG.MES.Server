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
				IsTwoSidePaint = x.po.IsTwoSidePaint
			})
			.FirstOrDefaultAsync();

		return result;
	}

	public async Task<OrderDetailDto?> GetOrderByNumberAsync(string orderNumber)
	{
		return await GetOrderByIdentifierQuery()
			.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
	}

	public async Task<List<OrderTraceDto>> GetOrderTraceByNumberAsync(string orderNumber)
	{
		var orders = await _context.Orders
			.Where(o => o.OrderNumber == orderNumber)
			.ToListAsync();

		var traces = new List<OrderTraceDto>();

		foreach (var order in orders)
		{
			var productionOrder = await _context.ProductionOrders
				.FirstOrDefaultAsync(po => po.OrderId == order.Id);

			if (productionOrder == null)
			{
				traces.Add(new OrderTraceDto
				{
					OrderId = order.Id,
					OrderNumber = order.OrderNumber,
					ReadyDate = order.ReadyDate,
					Status = "not_started",
					Workplaces = new List<WorkplaceTraceDto>()
				});
				continue;
			}

			var footprints = await _context.OrderFootprints
				.Where(fp => fp.ProductionOrderId == productionOrder.Id)
				.Join(_context.Workplaces, fp => fp.WorkplaceId, w => w.Id, (fp, w) => new WorkplaceTraceDto
				{
					WorkplaceId = fp.WorkplaceId,
					WorkplaceName = w.Name,
					Status = fp.Status
				})
				.ToListAsync();

			traces.Add(new OrderTraceDto
			{
				OrderId = order.Id,
				ProductionOrderId = productionOrder.Id,
				OrderNumber = order.OrderNumber,
				ReadyDate = order.ReadyDate,
				Status = "in_progress",
				Workplaces = footprints
			});
		}

		return traces;
	}


	public async Task<List<OrderWorkplaceDto>> GetPendingOrdersForWorkplaceAsync(Guid workplaceId)
	{
		var isStart = await OrderServiceHelper.IsStartWorkplaceAsync(_context, workplaceId);

		if (isStart)
		{
			var noneId = await OrderServiceHelper.GetNoneWorkplaceIdAsync(_context);

			var newOrders = await _context.ProductionOrders
				.Where(po => po.CurrentWorkplaceId == noneId)
				.Join(_context.Orders, po => po.OrderId, o => o.Id, (po, o) => new OrderWorkplaceDto
				{
					ProductionOrderId = po.Id,
					WorkplaceId = workplaceId,
					Status = OrderStatus.WorkplaceStatus.Pending,
					OrderId = o.Id,
					OrderNumber = o.OrderNumber,
					WindowCount = o.WindowCount,
					WindowArea = o.WindowArea,
					PlateCount = o.PlateCount,
					PlateArea = o.PlateArea,
					ReadyDate = o.ReadyDate,
					IsEconom = o.IsEconom,
					IsClaim = o.IsClaim,
					IsOnlyPaid = o.IsOnlyPaid
				})
				.ToListAsync();

			return newOrders;
		}

		var pendingOrders = await _context.OrderFootprints
			.Where(fp => fp.WorkplaceId == workplaceId &&
						(fp.Status == OrderStatus.WorkplaceStatus.Pending ||
						 fp.Status == OrderStatus.WorkplaceStatus.Joinery))
			.Join(_context.ProductionOrders, fp => fp.ProductionOrderId, po => po.Id, (fp, po) => new { fp, po })
			.Join(_context.Orders, x => x.po.OrderId, o => o.Id, (x, o) => new OrderWorkplaceDto
			{
				ProductionOrderId = x.fp.ProductionOrderId,
				WorkplaceId = x.fp.WorkplaceId,
				Status = x.fp.Status,
				OrderId = o.Id,
				OrderNumber = o.OrderNumber,
				WindowCount = o.WindowCount,
				WindowArea = o.WindowArea,
				PlateCount = o.PlateCount,
				PlateArea = o.PlateArea,
				ReadyDate = o.ReadyDate,
				IsEconom = o.IsEconom,
				IsClaim = o.IsClaim,
				IsOnlyPaid = o.IsOnlyPaid
			})
			.ToListAsync();

		return pendingOrders;
	}
}