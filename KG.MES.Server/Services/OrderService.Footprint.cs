using KG.MES.Server.Constants;
using KG.MES.Server.Hubs;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public partial class OrderService
{
	public async Task<List<OrderWorkplaceDto>> GetActiveOrdersForWorkplaceAsync(Guid workplaceId)
	{
		var activeOrders = await _context.OrderFootprints
			.Where(fp => fp.WorkplaceId == workplaceId && fp.Status == OrderStatus.WorkplaceStatus.Active)
			.Join(_context.ProductionOrders, fp => fp.ProductionOrderId, po => po.Id, (fp, po) => new { fp, po })
			.Join(_context.Orders, x => x.po.OrderId, o => o.Id, (x, o) => new OrderWorkplaceDto
			{
				Id = x.fp.ProductionOrderId,
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
				IsOnlyPaid = o.IsOnlyPaid,
				WorkplaceOrderStatus = x.fp.Status,
				FromJoinery = x.fp.Status == "joinery",
				Name = o.OrderNumber
			})
			.ToListAsync();

		return activeOrders;
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
					Id = po.Id,
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
					IsOnlyPaid = o.IsOnlyPaid,
					WorkplaceOrderStatus = OrderStatus.WorkplaceStatus.Pending,
					FromJoinery = false,
					Name = o.OrderNumber
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
				Id = x.fp.ProductionOrderId,
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
				IsOnlyPaid = o.IsOnlyPaid,
				WorkplaceOrderStatus = x.fp.Status,
				FromJoinery = x.fp.Status == "joinery",
				Name = x.fp.Status == "joinery" ? $"🪚 {o.OrderNumber}" : o.OrderNumber
			})
			.ToListAsync();

		return pendingOrders;
	}


	public async Task<List<OrderWorkplaceDto>> GetActiveAndPendingOrdersForWorkplaceAsync(Guid workplaceId)
	{
		var isStart = await OrderServiceHelper.IsStartWorkplaceAsync(_context, workplaceId);
		var result = new List<OrderWorkplaceDto>();

		if (isStart)
		{
			var noneId = await OrderServiceHelper.GetNoneWorkplaceIdAsync(_context);

			var newOrders = await _context.ProductionOrders
				.Where(po => po.CurrentWorkplaceId == noneId)
				.Join(_context.Orders, po => po.OrderId, o => o.Id, (po, o) => new OrderWorkplaceDto
				{
					Id = po.Id,
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
					IsOnlyPaid = o.IsOnlyPaid,
					WorkplaceOrderStatus = OrderStatus.WorkplaceStatus.Pending,
					FromJoinery = false,
					Name = o.OrderNumber
				})
				.ToListAsync();

			result.AddRange(newOrders);
		}

		var allOrders = await _context.OrderFootprints
			.Where(fp => fp.WorkplaceId == workplaceId &&
						(fp.Status == OrderStatus.WorkplaceStatus.Pending ||
						 fp.Status == OrderStatus.WorkplaceStatus.Joinery ||
						 fp.Status == OrderStatus.WorkplaceStatus.Active))
			.Join(_context.ProductionOrders, fp => fp.ProductionOrderId, po => po.Id, (fp, po) => new { fp, po })
			.Join(_context.Orders, x => x.po.OrderId, o => o.Id, (x, o) => new OrderWorkplaceDto
			{
				Id = x.fp.ProductionOrderId,
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
				IsOnlyPaid = o.IsOnlyPaid,
				WorkplaceOrderStatus = x.fp.Status,
				FromJoinery = x.fp.Status == "joinery",
				Name = x.fp.Status == "joinery" ? $"🪚 {o.OrderNumber}" : o.OrderNumber
			})
			.ToListAsync();

		result.AddRange(allOrders);
		return result;
	}

	public async Task<SetFootprintResultDto> SetOrderFootprintStatusAsync(
		Guid productionOrderId, Guid workplaceId, string status, Guid? userId, string notes)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var hasExisting = await _context.OrderFootprints
				.AnyAsync(fp => fp.ProductionOrderId == productionOrderId);

			string? oldStatus = null;

			if (!hasExisting)
			{
				await BuildFullPathAsync(productionOrderId, workplaceId);
			}
			else
			{
				var existing = await _context.OrderFootprints
					.FirstOrDefaultAsync(fp => fp.ProductionOrderId == productionOrderId && fp.WorkplaceId == workplaceId);

				if (existing != null)
				{
					oldStatus = existing.Status;
					existing.Status = status;
					existing.UpdatedAt = DateTime.UtcNow;
				}
			}

			var productionOrder = await _context.ProductionOrders
				.FirstOrDefaultAsync(po => po.Id == productionOrderId);

			if (productionOrder != null)
			{
				productionOrder.CurrentWorkplaceId = workplaceId;
				productionOrder.UpdatedAt = DateTime.UtcNow;
			}

			await _context.SaveChangesAsync();

			if (userId.HasValue)
			{
				var operationLog = new OperationLog
				{
					Id = Guid.NewGuid(),
					ProductionOrderId = productionOrderId,
					WorkplaceId = workplaceId,
					UserId = userId,
					OperationType = "MANUAL_UPDATE",
					OperationTime = DateTime.UtcNow,
					Notes = $"Статус изменён с {oldStatus ?? "NULL"} на {status}. {notes}",
					Source = "API"
				};
				_context.OperationLogs.Add(operationLog);
				await _context.SaveChangesAsync();
			}

			await transaction.CommitAsync();

			return new SetFootprintResultDto
			{
				Success = true,
				Message = $"Status updated to '{status}'",
				OldStatus = oldStatus,
				IsNew = !hasExisting
			};
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error in SetOrderFootprintStatus");
			throw;
		}
	}

	private async Task BuildFullPathAsync(Guid productionOrderId, Guid startWorkplaceId)
	{
		var allWorkplaces = await _context.Workplaces
			.Where(w => w.IsWorkplace)
			.Select(w => w.Id)
			.ToListAsync();

		foreach (var wpId in allWorkplaces)
		{
			var status = wpId == startWorkplaceId
				? OrderStatus.WorkplaceStatus.Active
				: OrderStatus.WorkplaceStatus.Planned;

			var footprint = new OrderFootprint
			{
				Id = Guid.NewGuid(),
				ProductionOrderId = productionOrderId,
				WorkplaceId = wpId,
				Status = status,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_context.OrderFootprints.Add(footprint);
		}

		await _context.SaveChangesAsync();

		await ActivateNextWorkplacesAsync(productionOrderId, startWorkplaceId);
		await ActivateParallelWorkplacesAsync(productionOrderId, startWorkplaceId);
	}

	private async Task ActivateNextWorkplacesAsync(Guid productionOrderId, Guid startedWorkplaceId)
	{
		var isJoinery = await OrderServiceHelper.IsJoineryWorkplaceAsync(_context, startedWorkplaceId);
		var nextWorkplaces = await OrderServiceHelper.GetNextWorkplacesAsync(_context, startedWorkplaceId);

		foreach (var workplaceId in nextWorkplaces)
		{
			var newStatus = isJoinery
				? OrderStatus.WorkplaceStatus.Joinery
				: OrderStatus.WorkplaceStatus.Pending;

			await UpdateStatusAsync(productionOrderId, workplaceId, newStatus);
		}
	}

	private async Task ActivateParallelWorkplacesAsync(Guid productionOrderId, Guid startedWorkplaceId)
	{
		var parallelWorkplaces = await OrderServiceHelper.GetParallelWorkplacesAsync(_context, startedWorkplaceId);

		foreach (var workplaceId in parallelWorkplaces)
		{
			if (workplaceId == startedWorkplaceId)
				continue;

			await UpdateStatusAsync(productionOrderId, workplaceId, OrderStatus.WorkplaceStatus.Pending);
		}
	}

	private async Task UpdateStatusAsync(Guid productionOrderId, Guid workplaceId, string newStatus)
	{
		var footprint = await _context.OrderFootprints
			.FirstOrDefaultAsync(fp => fp.ProductionOrderId == productionOrderId && fp.WorkplaceId == workplaceId);

		if (footprint == null)
			return;

		if (OrderStatus.WorkplaceStatus.CanTransition(footprint.Status, newStatus))
		{
			footprint.Status = newStatus;
			footprint.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
		}

		// Публикую события в SignalR
		try
		{
			if (productionOrderId != Guid.Empty)
			{
				// 1. Оповещение для карточки заказа (обновятся открытые карточки)
				await NotificationHelper.OrderUpdated(productionOrderId, workplaceId, newStatus);

				// 2. Оповещение для рабочего места (обновятся списки в приложениях)
				await NotificationHelper.WorkplaceOrderUpdated(workplaceId, productionOrderId, newStatus);

				_logger.LogInformation(
					"SignalR: order {OrderId}, workplace {WorkplaceId}, status → {NewStatus}",
					productionOrderId, workplaceId, newStatus);
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to send SignalR notification for status change");
		}

	}
}