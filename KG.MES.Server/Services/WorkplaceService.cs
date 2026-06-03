// KG.MES.Server/Services/WorkplaceService.cs
using KG.MES.Server.Constants;
using KG.MES.Server.Data;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public class WorkplaceService : IWorkplaceService
{
	private readonly AppDbContext _context;
	private readonly ILogger<WorkplaceService> _logger;

	public WorkplaceService(AppDbContext context, ILogger<WorkplaceService> logger)
	{
		_context = context;
		_logger = logger;
	}

	public async Task<List<WorkplaceDto>> GetActiveWorkplacesAsync()
	{
		return await _context.Workplaces
			.Where(w => w.IsWorkplace)
			.OrderBy(w => w.Name)
			.Select(w => new WorkplaceDto
			{
				Id = w.Id,
				Name = w.Name,
				IsWorkplace = w.IsWorkplace
			})
			.ToListAsync();
	}

	public async Task<List<WorkplaceDto>> GetAllWorkplacesAsync()
	{
		return await _context.Workplaces
			.OrderBy(w => w.Name)
			.Select(w => new WorkplaceDto
			{
				Id = w.Id,
				Name = w.Name,
				IsWorkplace = w.IsWorkplace
			})
			.ToListAsync();
	}

	public async Task<WorkplaceStatsDto> GetWorkplaceStatsAsync(Guid workplaceId)
	{
		// Статистика из футпринта
		var statsQuery = await _context.OrderFootprints
			.Where(fp => fp.WorkplaceId == workplaceId)
			.GroupBy(fp => 1)
			.Select(g => new
			{
				PendingCount = g.Count(fp => fp.Status == OrderStatus.WorkplaceStatus.Pending),
				JoineryCount = g.Count(fp => fp.Status == OrderStatus.WorkplaceStatus.Joinery),
				ActiveCount = g.Count(fp => fp.Status == OrderStatus.WorkplaceStatus.Active),
				CompletedCount = g.Count(fp => fp.Status == OrderStatus.WorkplaceStatus.Completed)
			})
			.FirstOrDefaultAsync();

		// Активные блокировки
		var activeBlocks = await _context.OrderBlocks
			.CountAsync(b => b.WorkplaceId == workplaceId && b.ResolvedAt == null);

		// Активные заказы
		var activeOrders = await _context.OrderFootprints
			.Where(fp => fp.WorkplaceId == workplaceId && fp.Status == OrderStatus.WorkplaceStatus.Active)
			.Join(_context.ProductionOrders, fp => fp.ProductionOrderId, po => po.Id, (fp, po) => new { fp, po })
			.Join(_context.Orders, x => x.po.OrderId, o => o.Id, (x, o) => new ActiveOrderDto
			{
				OrderNumber = o.OrderNumber,
				StartedAt = x.fp.CreatedAt,
				HoursInWork = (DateTime.UtcNow - x.fp.CreatedAt).TotalHours
			})
			.OrderByDescending(o => o.StartedAt)
			.ToListAsync();

		// Проверяем, является ли участок стартовым
		var isStart = await OrderServiceHelper.IsStartWorkplaceAsync(_context, workplaceId);
		var pendingCount = statsQuery?.PendingCount ?? 0;

		if (isStart)
		{
			var noneId = await OrderServiceHelper.GetNoneWorkplaceIdAsync(_context);
			var newOrdersCount = await _context.ProductionOrders
				.CountAsync(po => po.CurrentWorkplaceId == noneId &&
					!_context.OrderFootprints.Any(fp => fp.ProductionOrderId == po.Id));
			pendingCount += newOrdersCount;
		}

		return new WorkplaceStatsDto
		{
			PendingCount = pendingCount,
			JoineryCount = statsQuery?.JoineryCount ?? 0,
			ActiveCount = statsQuery?.ActiveCount ?? 0,
			CompletedCount = statsQuery?.CompletedCount ?? 0,
			ActiveBlocks = activeBlocks,
			ActiveOrders = activeOrders
		};
	}

	public async Task<List<WorkplaceHistoryDto>> GetWorkplaceHistoryAsync(Guid workplaceId, DateTime? from, DateTime? to, int limit = 50)
	{
		var query = _context.OperationLogs
			.Where(ol => ol.WorkplaceId == workplaceId)
			.Join(_context.ProductionOrders, ol => ol.ProductionOrderId, po => po.Id, (ol, po) => new { ol, po })
			.Join(_context.Orders, x => x.po.OrderId, o => o.Id, (x, o) => new WorkplaceHistoryDto
			{
				OperationTime = x.ol.OperationTime,
				OperationType = x.ol.OperationType,
				OrderNumber = o.OrderNumber,
				UserName = _context.Users.Where(u => u.Id == x.ol.UserId).Select(u => u.Name).FirstOrDefault(),
				Notes = x.ol.Notes
			});

		if (from.HasValue)
			query = query.Where(h => h.OperationTime >= from.Value);

		if (to.HasValue)
			query = query.Where(h => h.OperationTime <= to.Value);

		return await query
			.OrderByDescending(h => h.OperationTime)
			.Take(limit)
			.ToListAsync();
	}

	public async Task<List<WorkplaceBlockDto>> GetWorkplaceBlocksAsync(Guid workplaceId)
	{
		return await _context.OrderBlocks
			.Where(b => b.WorkplaceId == workplaceId && b.ResolvedAt == null)
			.Join(_context.ProductionOrders, b => b.ProductionOrderId, po => po.Id, (b, po) => new { b, po })
			.Join(_context.Orders, x => x.po.OrderId, o => o.Id, (x, o) => new WorkplaceBlockDto
			{
				Id = x.b.Id,
				ProductionOrderId = x.b.ProductionOrderId,
				OrderNumber = o.OrderNumber,
				Reason = x.b.Reason,
				BlockedAt = x.b.BlockedAt,
				UserName = _context.Users.Where(u => u.Id == x.b.UserId).Select(u => u.Name).FirstOrDefault()
			})
			.OrderByDescending(b => b.BlockedAt)
			.ToListAsync();
	}
}