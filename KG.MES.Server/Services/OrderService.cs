using System.Globalization;
using KG.MES.Server.Data;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public partial class OrderService : IOrderService
{
	private readonly AppDbContext _context;
	private readonly ILogger<OrderService> _logger;
	private readonly OrderAttributeService _orderAttributeService;

	public OrderService(AppDbContext context, ILogger<OrderService> logger, OrderAttributeService orderAttributeService)
	{
		_context = context;
		_logger = logger;
		_orderAttributeService = orderAttributeService;
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
				IsTwoSidePaint = x.po.IsTwoSidePaint,
				Machine = x.po.Machine
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
				CurrentStatus = w.Name,
				Machine = x.po.Machine
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

	public async Task<SetOrderStatusResultDto> SetOrderStatusAsync(
		Guid productionOrderId, string targetStatusName, Guid? userId, string? notes)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var status = await _context.Workplaces
				.FirstOrDefaultAsync(w => w.Name == targetStatusName);

			if (status == null)
			{
				return new SetOrderStatusResultDto
				{
					Success = false,
					Message = "Статус 'Отгружен' не найден в справочнике"
				};
			}


			var productionOrder = await _context.ProductionOrders
				.FirstOrDefaultAsync(po => po.Id == productionOrderId);

			if (productionOrder != null)
			{
				productionOrder.CurrentWorkplaceId = status.Id;
				productionOrder.UpdatedAt = DateTime.UtcNow;
			}

			await _context.SaveChangesAsync();

			await transaction.CommitAsync();

			return new SetOrderStatusResultDto
			{
				Success = true,
				Message = $"Status updated to '{status.Name}'"
			};
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error in SetOrderFootprintStatus");
			throw;
		}
	}

	public async Task<SetOrderStatusResultDto> SetOrderCompleteAsync(
		Guid OrderId, Guid? userId, string? notes)
	{
		try
		{
			return await SetOrderStatusAsync(OrderId, Constants.OrderStatus.CommonStatus.Complete, userId, notes);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in SetOrderCompleteAsync");
			return new SetOrderStatusResultDto
			{
				Success = false,
				Message = ex.Message
			};
		}
	}

	public async Task<SetOrderStatusResultDto> SetOrderDepartureAsync(
		Guid OrderId, Guid? userId, string? notes)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var result = await SetOrderStatusAsync(OrderId, Constants.OrderStatus.CommonStatus.Departure, userId, notes);

			if (!result.Success)
			{
				await transaction.RollbackAsync();
				return result;
			}
			// Удаляем следы заказа (footprint)

			var productionOrder = await _context.ProductionOrders
				.FirstOrDefaultAsync(po => po.OrderId == OrderId);

			var Order = await _context.Orders
				.Include(o => o.ProductionOrder)
				.FirstOrDefaultAsync(o => o.Id == OrderId);


			var footprints = await _context.OrderFootprints
				.Where(fp => fp.ProductionOrderId == Order!.ProductionOrder!.Id)
				.ToListAsync();

			if (footprints.Any())
			{
				_context.OrderFootprints.RemoveRange(footprints);
				await _context.SaveChangesAsync();
			}

			await transaction.CommitAsync();

			return new SetOrderStatusResultDto
			{
				Success = true,
				Message = "Заказ отгружен, следы удалены"
			};
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error in SetOrderDepartureAsync");
			return new SetOrderStatusResultDto
			{
				Success = false,
				Message = ex.Message
			};
		}
	}
}