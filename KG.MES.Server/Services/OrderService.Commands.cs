using KG.MES.Server.Constants;
using KG.MES.Server.Controllers;
using KG.MES.Server.Models.Dto;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using KG.MES.Server.Hubs;

namespace KG.MES.Server.Services;

public partial class OrderService
{
	public async Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderRequestDto request)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var order = new Order
			{
				Id = Guid.NewGuid(),
				OrderNumber = request.OrderNumber,
				ReadyDate = request.ReadyDate,
				WindowCount = request.WindowCount,
				WindowArea = request.WindowArea,
				PlateCount = request.PlateCount,
				PlateArea = request.PlateArea,
				IsEconom = request.IsEconom,
				IsClaim = request.IsClaim,
				IsOnlyPaid = request.IsOnlyPaid,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				CommentIds = new List<Guid>()
			};

			_context.Orders.Add(order);
			await _context.SaveChangesAsync();

			var noneId = await OrderServiceHelper.GetNoneWorkplaceIdAsync(_context);

			var productionOrder = new ProductionOrder
			{
				Id = Guid.NewGuid(),
				OrderId = order.Id,
				CurrentWorkplaceId = noneId,
				Comment = request.Comment,
				Lumber = request.Lumber,
				GlazingBead = request.GlazingBead,
				IsTwoSidePaint = request.IsTwoSidePaint,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				CommentIds = new List<Guid>()
			};

			_context.ProductionOrders.Add(productionOrder);
			await _context.SaveChangesAsync();

			var orderSupply = new OrderSupply
			{
				Id = Guid.NewGuid(),
				OrderId = order.Id,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				CommentIds = new List<Guid>()
			};

			_context.OrderSupplies.Add(orderSupply);
			await _context.SaveChangesAsync();

			var supplyTypes = await _context.SupplyTypes
				.Where(st => st.IsActive)
				.ToListAsync();

			var supplyItemIds = new List<Guid>();

			foreach (var supplyType in supplyTypes)
			{
				var supplyItem = new SupplyItem
				{
					Id = Guid.NewGuid(),
					OrderSupplyId = orderSupply.Id,
					SupplyTypeId = supplyType.Id,
					ConditionId = null,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};

				_context.SupplyItems.Add(supplyItem);
				supplyItemIds.Add(supplyItem.Id);
			}

			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			return new CreateOrderResultDto
			{
				Success = true,
				OrderId = order.Id,
				ProductionOrderId = productionOrder.Id,
				OrderSupplyId = orderSupply.Id,
				SupplyItemIds = supplyItemIds
			};
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error in CreateOrderAsync");
			throw;
		}
	}

	public async Task<OperationResultDto> BeginOrderWorkplaceAsync(
		Guid productionOrderId, Guid workplaceId, Guid userId, string notes, string source)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var hasExisting = await _context.OrderFootprints
				.AnyAsync(fp => fp.ProductionOrderId == productionOrderId);

			if (!hasExisting)
			{
				await BuildFullPathAsync(productionOrderId, workplaceId);
			}
			else
			{
				await UpdateStatusAsync(productionOrderId, workplaceId, OrderStatus.WorkplaceStatus.Active);
			}

			var operationLog = new OperationLog
			{
				Id = Guid.NewGuid(),
				ProductionOrderId = productionOrderId,
				WorkplaceId = workplaceId,
				UserId = userId,
				OperationType = "START",
				OperationTime = DateTime.UtcNow,
				Notes = notes,
				Source = source,
				CreatedAt = DateTime.UtcNow
			};

			_context.OperationLogs.Add(operationLog);

			var productionOrder = await _context.ProductionOrders
				.FirstOrDefaultAsync(po => po.Id == productionOrderId);

			if (productionOrder != null)
			{
				productionOrder.CurrentWorkplaceId = workplaceId;
				productionOrder.UpdatedAt = DateTime.UtcNow;
			}

			await _context.SaveChangesAsync();

			await ActivateNextWorkplacesAsync(productionOrderId, workplaceId);
			await ActivateParallelWorkplacesAsync(productionOrderId, workplaceId);

			await transaction.CommitAsync();

			//await NotificationHelper.OrderUpdated(productionOrderId, workplaceId, OrderStatus.WorkplaceStatus.Active, userId);
			//await NotificationHelper.WorkplaceOrderUpdated(workplaceId, productionOrderId, OrderStatus.WorkplaceStatus.Active);

			return new OperationResultDto
			{
				Success = true,
				Message = "Order started"
			};
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error in BeginOrderWorkplaceAsync");
			throw;
		}
	}

	public async Task<OperationResultDto> CompleteOrderWorkplaceAsync(
		Guid productionOrderId, Guid workplaceId, Guid userId, string notes, string source)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			await UpdateStatusAsync(productionOrderId, workplaceId, OrderStatus.WorkplaceStatus.Completed);

			var operationLog = new OperationLog
			{
				Id = Guid.NewGuid(),
				ProductionOrderId = productionOrderId,
				WorkplaceId = workplaceId,
				UserId = userId,
				OperationType = "COMPLETE",
				OperationTime = DateTime.UtcNow,
				Notes = notes,
				Source = source,
				CreatedAt = DateTime.UtcNow
			};

			_context.OperationLogs.Add(operationLog);
			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			//await NotificationHelper.OrderUpdated(productionOrderId, workplaceId, OrderStatus.WorkplaceStatus.Completed, userId);
			//await NotificationHelper.WorkplaceOrderUpdated(workplaceId, productionOrderId, OrderStatus.WorkplaceStatus.Completed);

			return new OperationResultDto
			{
				Success = true,
				Message = "Order completed"
			};
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error in CompleteOrderWorkplaceAsync");
			throw;
		}
	}

	public async Task<BatchUpdateResultDto> UpdateOrderFootprintBatchAsync(
		Guid productionOrderId, List<FootprintItemDto> footprints, Guid? userId, string notes)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();
		var details = new List<BatchUpdateDetail>();

		try
		{
			foreach (var footprint in footprints)
			{
				var existing = await _context.OrderFootprints
					.FirstOrDefaultAsync(fp => fp.ProductionOrderId == productionOrderId && fp.WorkplaceId == footprint.WorkplaceId);

				string? oldStatus = null;

				if (existing == null)
				{
					_context.OrderFootprints.Add(new OrderFootprint
					{
						Id = Guid.NewGuid(),
						ProductionOrderId = productionOrderId,
						WorkplaceId = footprint.WorkplaceId,
						Status = footprint.Status,
						CreatedAt = DateTime.UtcNow,
						UpdatedAt = DateTime.UtcNow
					});
					details.Add(new BatchUpdateDetail
					{
						WorkplaceId = footprint.WorkplaceId,
						Success = true,
						OldStatus = null,
						NewStatus = footprint.Status
					});
				}
				else
				{
					oldStatus = existing.Status;

					if (OrderStatus.WorkplaceStatus.CanTransition(oldStatus, footprint.Status))
					{
						existing.Status = footprint.Status;
						existing.UpdatedAt = DateTime.UtcNow;
						details.Add(new BatchUpdateDetail
						{
							WorkplaceId = footprint.WorkplaceId,
							Success = true,
							OldStatus = oldStatus,
							NewStatus = footprint.Status
						});
					}
					else
					{
						details.Add(new BatchUpdateDetail
						{
							WorkplaceId = footprint.WorkplaceId,
							Success = false,
							OldStatus = oldStatus,
							NewStatus = footprint.Status,
							Error = $"Cannot transition from '{oldStatus}' to '{footprint.Status}'"
						});
					}
				}
			}

			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			return new BatchUpdateResultDto
			{
				Success = true,
				Message = $"Updated {details.Count(d => d.Success)} footprints",
				Details = details
			};
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error in UpdateOrderFootprintBatchAsync");
			throw;
		}
	}
}