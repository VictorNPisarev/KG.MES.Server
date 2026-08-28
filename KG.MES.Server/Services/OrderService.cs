using System.Globalization;
using KG.MES.Shared.Constants;
using KG.MES.Shared.Data;
using KG.MES.Shared.Extensions;

//using KG.MES.Server.Extensions;
using KG.MES.Shared.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Shared.Services;

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
				CreatedAt = x.o.CreatedAt.ToProductionTime(),
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
	public async Task<PaginatedResponse<OrderDto>> GetOrdersAsync(
		int page, int limit, string? sortBy, string? sortOrder, List<Guid>? workplaceIds, string? orderNumber)
	{
		var query = _context.Orders
			.Join(_context.ProductionOrders, o => o.Id, po => po.OrderId, (o, po) => new { o, po })
			.Join(_context.Workplaces, x => x.po.CurrentWorkplaceId, w => w.Id, (x, w) => new OrderDto
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
				IsTwoSidePaint = x.po.IsTwoSidePaint,
				CreatedAt = x.o.CreatedAt,
				ProductionOrderId = x.po.Id,
				CurrentWorkplaceId = x.po.CurrentWorkplaceId,
				CurrentStatus = w.Name,
				Machine = x.po.Machine,
				RtmDate = x.o.RtmDate
			});

		if (!string.IsNullOrEmpty(orderNumber))
			query = query.Where(o => EF.Functions.ILike(o.OrderNumber, $"%{orderNumber}%"));

		if (
			//string.IsNullOrEmpty(orderNumber) &&
			workplaceIds != null && workplaceIds.Any())
		{
			query = query.Where(o => workplaceIds.Contains(o.CurrentWorkplaceId ?? Guid.Empty));
		}

		var total = await query.CountAsync();

		// Применяем сортировку
		IOrderedQueryable<OrderDto> orderedQuery;

		orderedQuery = query.OrderByProperty(sortBy, sortOrder);


		var items = await orderedQuery
			.Skip((page - 1) * limit)
			.Take(limit)
			.ToListAsync();

		foreach (var item in items)
		{
			item.CreatedAt = item.CreatedAt.ToProductionTime();
		}

		return new PaginatedResponse<OrderDto>
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
		Guid orderId, string targetStatusName, Guid? userId, string? notes)
	{
		var status = await _context.Workplaces
			.FirstOrDefaultAsync(w => w.Name == targetStatusName);

		if (status == null)
		{
			return new SetOrderStatusResultDto
			{
				Success = false,
				Message = $"Статус '{targetStatusName}' не найден в справочнике"
			};
		}

		var productionOrder = await _context.ProductionOrders
			.FirstOrDefaultAsync(po => po.OrderId == orderId);

		if (productionOrder == null)
		{
			return new SetOrderStatusResultDto
			{
				Success = false,
				Message = $"Производственный заказ для OrderId '{orderId}' не найден"
			};
		}

		var oldWorkplaceId = productionOrder.CurrentWorkplaceId;
		productionOrder.CurrentWorkplaceId = status.Id;
		productionOrder.UpdatedAt = DateTime.UtcNow;

		var operationLog = new OperationLog
		{
			Id = Guid.NewGuid(),
			ProductionOrderId = productionOrder.Id,
			WorkplaceId = status.Id,
			UserId = userId,
			OperationType = "COMPLETE",
			OperationTime = DateTime.UtcNow,
			Notes = notes ?? $"Статус изменен на '{targetStatusName}'",
			Source = "SetOrderStatusAsync",
			CreatedAt = DateTime.UtcNow
		};

		_context.OperationLogs.Add(operationLog);
		await _context.SaveChangesAsync();

		return new SetOrderStatusResultDto
		{
			Success = true,
			Message = $"Status updated to '{status.Name}'"
		};
	}

	public async Task<SetOrderStatusResultDto> SetOrderCompleteAsync(
		Guid orderId, Guid? userId, string? notes)
	{
		var orderExists = await _context.Orders.AnyAsync(o => o.Id == orderId);
		if (!orderExists)
		{
			_logger.LogWarning($"Order {orderId} not found");
			return new SetOrderStatusResultDto
			{
				Success = false,
				Message = $"Заказ {orderId} не найден"
			};
		}

		//using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var result = await SetOrderStatusAsync(orderId, Constants.OrderStatus.CommonStatus.Complete, userId, notes);
			
			if (!result.Success)
			{
				//await transaction.RollbackAsync();
				return result;
			}
			// Меняю все следы заказа (footprint) на completed

			var Order = await _context.Orders
				.Include(o => o.ProductionOrder)
				.FirstOrDefaultAsync(o => o.Id == orderId);


			var footprints = await _context.OrderFootprints
				.Where(fp => fp.ProductionOrderId == Order!.ProductionOrder!.Id)
				.Where(fp => fp.Status != Constants.OrderStatus.WorkplaceStatus.Completed)
				.ToListAsync();

			if (footprints.Any())
			{
				foreach (var footprint in footprints)
				{
					footprint.Status = Constants.OrderStatus.WorkplaceStatus.Completed;

					_context.OperationLogs.Add(
						new OperationLog
						{
							Id = Guid.NewGuid(),
							ProductionOrderId = footprint.ProductionOrderId,
							WorkplaceId = footprint.WorkplaceId,
							UserId = userId, //TODO
							OperationType = "COMPLETE", //TODO
							OperationTime = DateTime.UtcNow,
							CreatedAt = DateTime.UtcNow,
							Notes = "Цикл производства завершен",
							Source = "Handle change by Order COMPLETE"
						}
					);
					await _context.SaveChangesAsync();
				}
			}

			//await transaction.CommitAsync();

			return new SetOrderStatusResultDto
			{
				Success = true,
				Message = "Заказ отгружен, следы завершены"
			};
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
		Guid orderId, Guid? userId, string? notes)
	{

		var orderExists = await _context.Orders.AnyAsync(o => o.Id == orderId);
		if (!orderExists)
		{
			_logger.LogWarning($"Order {orderId} not found");
			return new SetOrderStatusResultDto
			{
				Success = false,
				Message = $"Заказ {orderId} не найден"
			};
		}

		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var result = await SetOrderStatusAsync(orderId, Constants.OrderStatus.CommonStatus.Departure, userId, notes);

			if (!result.Success)
			{
				await transaction.RollbackAsync();
				return result;
			}
			// Удаляю следы заказа (footprint)

			var productionOrder = await _context.ProductionOrders
				.FirstOrDefaultAsync(po => po.OrderId == orderId);

			var Order = await _context.Orders
				.Include(o => o.ProductionOrder)
				.FirstOrDefaultAsync(o => o.Id == orderId);


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

	// <summary>
	/// Обновляет CurrentWorkplaceId в production_orders на основе актуальных следов
	/// </summary>
	private async Task SetProductionOrderCurrentWorkplaceAsync(Guid productionOrderId, Guid workplaceId)
	{
		// Находим наиболее поздний участок со статусом active или completed
		// Берём последний по дате обновления или по уровню (если уровень есть)
		var latestFootprint = await _context.OrderFootprints
			.Where(fp => fp.ProductionOrderId == productionOrderId)
			.Where(fp => fp.Status == OrderStatus.WorkplaceStatus.Active ||
						 fp.Status == OrderStatus.WorkplaceStatus.Completed)
			.Join(_context.Workplaces, fp => fp.WorkplaceId, w => w.Id, (fp, w) => new { fp, w })
			.OrderByDescending(x => x.w.Level)  // ← по уровню
			.Select(x => x.fp)
			.FirstOrDefaultAsync();

		var productionOrder = await _context.ProductionOrders
			.FirstOrDefaultAsync(po => po.Id == productionOrderId);

		if (productionOrder == null)
			return;

		// Если есть активный/завершённый след — обновляем
		if (latestFootprint != null)
		{
			productionOrder.CurrentWorkplaceId = latestFootprint.WorkplaceId;
			productionOrder.UpdatedAt = DateTime.UtcNow;
		}
		else
		{
			productionOrder.CurrentWorkplaceId = workplaceId;
			productionOrder.UpdatedAt = DateTime.UtcNow;
		}

		await _context.SaveChangesAsync();
	}
}