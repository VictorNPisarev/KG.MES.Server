using KG.MES.Server.Data;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public partial class OrderService : IOrderService
{
	private readonly AppDbContext _context;
	private readonly ILogger<OrderService> _logger;

	public OrderService(AppDbContext context, ILogger<OrderService> logger)
	{
		_context = context;
		_logger = logger;
	}

	// Вспомогательные методы для сортировки
	private IOrderedQueryable<OrderListItemDto> OrderByOrderNumber(IQueryable<OrderListItemDto> query, string? sortOrder)
	{
		return sortOrder == "desc"
			? query.OrderByDescending(o => o.OrderNumber)
			: query.OrderBy(o => o.OrderNumber);
	}

	private IOrderedQueryable<OrderListItemDto> OrderByReadyDate(IQueryable<OrderListItemDto> query, string? sortOrder)
	{
		return sortOrder == "desc"
			? query.OrderByDescending(o => o.ReadyDate)
			: query.OrderBy(o => o.ReadyDate);
	}

	private IOrderedQueryable<OrderListItemDto> OrderByWindowCount(IQueryable<OrderListItemDto> query, string? sortOrder)
	{
		return sortOrder == "desc"
			? query.OrderByDescending(o => o.WindowCount)
			: query.OrderBy(o => o.WindowCount);
	}

	private IOrderedQueryable<OrderListItemDto> OrderByPlateCount(IQueryable<OrderListItemDto> query, string? sortOrder)
	{
		return sortOrder == "desc"
			? query.OrderByDescending(o => o.PlateCount)
			: query.OrderBy(o => o.PlateCount);
	}

	private IQueryable<OrderDetailDto> GetOrderByIdentifierQuery()
	{
		return _context.Orders
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
			});
	}

	// Основной метод GetOrdersAsync
	public async Task<PaginatedResponse<OrderListItemDto>> GetOrdersAsync(
		int page, int limit, string? sortBy, string? sortOrder, Guid? workplaceId, string? orderNumber)
	{
		var query = _context.Orders
			.Join(_context.ProductionOrders, o => o.Id, po => po.OrderId, (o, po) => new { o, po })
			.Join(_context.Workplaces, x => x.po.CurrentWorkplaceId, w => w.Id, (x, w) => new OrderListItemDto
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
				CurrentStatus = w.Name
			});

		if (workplaceId.HasValue)
			query = query.Where(o => o.CurrentWorkplaceId == workplaceId.Value);

		if (!string.IsNullOrEmpty(orderNumber))
			query = query.Where(o => EF.Functions.ILike(o.OrderNumber, $"%{orderNumber}%"));

		var total = await query.CountAsync();

		// Применяем сортировку
		IOrderedQueryable<OrderListItemDto> orderedQuery;

		switch (sortBy?.ToLower())
		{
			case "order_number":
				orderedQuery = OrderByOrderNumber(query, sortOrder);
				break;
			case "window_count":
				orderedQuery = OrderByWindowCount(query, sortOrder);
				break;
			case "plate_count":
				orderedQuery = OrderByPlateCount(query, sortOrder);
				break;
			case "ready_date":
			default:
				orderedQuery = OrderByReadyDate(query, sortOrder);
				break;
		}

		var items = await orderedQuery
			.Skip((page - 1) * limit)
			.Take(limit)
			.ToListAsync();

		return new PaginatedResponse<OrderListItemDto>
		{
			Data = items,
			Pagination = new PaginationInfo
			{
				Page = page,
				Limit = limit,
				Total = total,
				Pages = (int)Math.Ceiling(total / (double)limit)
			},
			Sort = new SortInfo
			{
				By = sortBy ?? "ready_date",
				Order = sortOrder ?? "asc"
			}
		};
	}
}