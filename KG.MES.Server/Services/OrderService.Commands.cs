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
	public async Task<CreateOrderResultDto> CreateOrderAsync(OrderRequestDto request)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			// 1. Создаем заказ (без CommentIds)
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
				RtmDate = request.RtmDate,
				So8Date = request.So8Date,
				ApprovedLeadDays = request.ApprovedLeadDays,
				UnapprovedLeadDays = request.UnapprovedLeadDays,
				CommentIds = []
			};

			_context.Orders.Add(order);
			await _context.SaveChangesAsync(); // Сохраняем, чтобы получить order.Id

			// 2. Создаем комментарий (если есть)
			Guid? commentId = null;
			if (!string.IsNullOrEmpty(request.Comment))
			{
				var comment = new Comment
				{
					Id = Guid.NewGuid(),
					OrderId = order.Id,
					UserId = null,
					Content = request.Comment,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};

				_context.Comments.Add(comment);
				await _context.SaveChangesAsync(); // Сохраняем комментарий

				// Добавляем ID комментария в заказ
				order.CommentIds.Add(comment.Id);
				commentId = comment.Id;
			}

			// 3. Создаем производственный заказ
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
				Machine = request.Machine,
				CommentIds = []
			};

			_context.ProductionOrders.Add(productionOrder);
			await _context.SaveChangesAsync();

			// 4. Создаем снабжение
			var orderSupply = new OrderSupply
			{
				Id = Guid.NewGuid(),
				OrderId = order.Id,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				CommentIds = []
			};

			_context.OrderSupplies.Add(orderSupply);
			await _context.SaveChangesAsync();

			// 5. Создаем позиции снабжения
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

			// ✅ 6. Уведомляем через SignalR о создании заказа
			try
			{
				// Уведомление для списка заказов
				//await NotificationHelper.OrderCreated(order.Id, order.OrderNumber);

				// Если есть комментарий, уведомляем о нем
				if (commentId.HasValue)
				{
					await NotificationHelper.OrderCommentAdded(
						order.Id,
						commentId.Value,
						request.Comment!,
						null);
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to send SignalR notification for order creation");
			}

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

			await SetProductionOrderCurrentWorkplaceAsync(productionOrderId, workplaceId);

			//var productionOrder = await _context.ProductionOrders
			//	.FirstOrDefaultAsync(po => po.Id == productionOrderId);

			//if (productionOrder != null)
			//{
			//	productionOrder.CurrentWorkplaceId = workplaceId;
			//	productionOrder.UpdatedAt = DateTime.UtcNow;
			//}

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
			var productionOrder = await _context.ProductionOrders
				.Include(po => po.CurrentWorkplace)
				.FirstOrDefaultAsync(po => po.Id == productionOrderId);

			var workplace = await _context.Workplaces
				.FirstOrDefaultAsync(w => w.Id == workplaceId);

			if (productionOrder == null)
			{
				return new OperationResultDto
				{
					Success = false,
					Message = "Order not found"
				};
			}

			await UpdateStatusAsync(productionOrder.Id, workplaceId, OrderStatus.WorkplaceStatus.Completed);

			if(workplace?.Code == WorkplaceCodes.Packing)
			{
				await SetOrderCompleteAsync(productionOrder.OrderId, null, null);
			}

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

	/// <summary>
	/// Получить заказ для редактирования
	/// </summary>
	public async Task<OrderRequestDto?> GetOrderForEditAsync(Guid orderId)
	{
		var order = await _context.Orders
			.FirstOrDefaultAsync(o => o.Id == orderId);

		if (order == null)
			return null;

		var productionOrder = await _context.ProductionOrders
			.FirstOrDefaultAsync(po => po.OrderId == orderId);

		if (productionOrder == null)
			return null;

		return new OrderRequestDto
		{
			//Id = order.Id,
			OrderNumber = order.OrderNumber,
			ReadyDate = order.ReadyDate,
			WindowCount = order.WindowCount,
			WindowArea = order.WindowArea,
			PlateCount = order.PlateCount,
			PlateArea = order.PlateArea,
			IsEconom = order.IsEconom,
			IsClaim = order.IsClaim,
			IsOnlyPaid = order.IsOnlyPaid,
			Comment = productionOrder.Comment,
			Lumber = productionOrder.Lumber,
			GlazingBead = productionOrder.GlazingBead,
			IsTwoSidePaint = productionOrder.IsTwoSidePaint,
			Machine = productionOrder.Machine,
			RtmDate = order.RtmDate,
			So8Date = order.So8Date,
			ApprovedLeadDays = order.ApprovedLeadDays ?? 0,
			UnapprovedLeadDays = order.UnapprovedLeadDays ?? 0
		};
	}

	/// <summary>
	/// Обновить заказ
	/// </summary>
	public async Task<bool> UpdateOrderAsync(Guid orderId, OrderRequestDto dto)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var order = await _context.Orders
				.FirstOrDefaultAsync(o => o.Id == orderId);

			if (order == null)
				return false;

			var productionOrder = await _context.ProductionOrders
				.FirstOrDefaultAsync(po => po.OrderId == orderId);

			if (productionOrder == null)
				return false;

			// Обновляем заказ
			order.OrderNumber = dto.OrderNumber;
			order.ReadyDate = dto.ReadyDate;
			order.WindowCount = dto.WindowCount;
			order.WindowArea = dto.WindowArea;
			order.PlateCount = dto.PlateCount;
			order.PlateArea = dto.PlateArea;
			order.IsEconom = dto.IsEconom;
			order.IsClaim = dto.IsClaim;
			order.IsOnlyPaid = dto.IsOnlyPaid;
			order.UpdatedAt = DateTime.UtcNow;
			order.RtmDate = dto.RtmDate;
			order.So8Date = dto.So8Date;
			order.ApprovedLeadDays = dto.ApprovedLeadDays;
			order.UnapprovedLeadDays = dto.UnapprovedLeadDays;

			// Обновляем производственный заказ
			productionOrder.Comment = dto.Comment;
			productionOrder.Lumber = dto.Lumber;
			productionOrder.GlazingBead = dto.GlazingBead;
			productionOrder.IsTwoSidePaint = dto.IsTwoSidePaint;
			productionOrder.Machine = dto.Machine;
			productionOrder.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			return true;
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error updating order {OrderId}", orderId);
			return false;
		}
	}

	/// <summary>
	/// Удалить заказ
	/// </summary>
	public async Task<bool> DeleteOrderAsync(Guid orderId)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var order = await _context.Orders
				.Include(o => o.ProductionOrder)
				.Include(o => o.OrderSupply)
					.ThenInclude(os => os!.SupplyItems)
				.FirstOrDefaultAsync(o => o.Id == orderId);

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();

			if (order == null)
			{
				Console.WriteLine("if (order == null)");
				return false;
			}

			Console.WriteLine("Order not null");
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();

			// Удаляем связанные данные (каскадно, но явно для контроля)
			if (order.OrderSupply != null)
			{
				// Удаляем supply_items (если есть)
				if (order.OrderSupply.SupplyItems != null)
				{
					_context.SupplyItems.RemoveRange(order.OrderSupply.SupplyItems);
				}
				_context.OrderSupplies.Remove(order.OrderSupply);
			}

			if (order.ProductionOrder != null)
			{
				// Удаляем footprints
				var footprints = await _context.OrderFootprints
					.Where(fp => fp.ProductionOrderId == order.ProductionOrder.Id)
					.ToListAsync();
				if (footprints.Any())
				{
					_context.OrderFootprints.RemoveRange(footprints);
				}

				_context.ProductionOrders.Remove(order.ProductionOrder);
			}

			_context.Orders.Remove(order);
			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			_logger.LogInformation("Order {OrderId} deleted", orderId);
			return true;
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error deleting order {OrderId}", orderId);
			return false;
		}
	}
}