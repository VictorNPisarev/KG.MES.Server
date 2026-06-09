// KG.MES.Server/Services/SupplyService.cs
using KG.MES.Server.Data;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;
using KG.MES.Server.Hubs;
using KG.MES.Server.Models.Dto;
using KG.MES.Shared.Models.Entities;

namespace KG.MES.Server.Services;

public class SupplyService : ISupplyService
{
	private readonly AppDbContext _context;
	private readonly ILogger<SupplyService> _logger;

	public SupplyService(AppDbContext context, ILogger<SupplyService> logger)
	{
		_context = context;
		_logger = logger;
	}

	public async Task<List<SupplyTypeDto>> GetSupplyTypesAsync()
	{
		return await _context.SupplyTypes
			.Where(st => st.IsActive)
			.Select(st => new SupplyTypeDto
			{
				Id = st.Id,
				Name = st.Name,
				DisplayName = st.DisplayName,
				Unit = st.Unit,
				IsActive = st.IsActive
			})
			.ToListAsync();
	}

	public async Task<List<SupplyConditionDto>> GetSupplyConditionsAsync()
	{
		return await _context.SupplyConditions
			.OrderBy(sc => sc.SortOrder)
			.Select(sc => new SupplyConditionDto
			{
				Id = sc.Id,
				ConditionCode = sc.ConditionCode,
				SortOrder = sc.SortOrder
			})
			.ToListAsync();
	}

	public async Task<List<OrderSupplyItemDto>> GetOrderSupplyItemsAsync(Guid orderId)
	{
		return await _context.OrderSupplies
			.Where(os => os.OrderId == orderId)
			.SelectMany(os => os.SupplyItems!)
			.Join(_context.SupplyTypes, si => si.SupplyTypeId, st => st.Id, (si, st) => new { si, st })
			.GroupJoin(_context.SupplyConditions, x => x.si.ConditionId, sc => sc.Id, (x, sc) => new { x.si, x.st, sc })
			.SelectMany(x => x.sc.DefaultIfEmpty(), (x, sc) => new { x.si, x.st, sc })
			.GroupJoin(_context.Comments, x => x.si.CommentId, c => c.Id, (x, c) => new { x.si, x.st, x.sc, c })
			.SelectMany(x => x.c.DefaultIfEmpty(), (x, c) => new OrderSupplyItemDto
			{
				OrderSupplyId = x.si.OrderSupplyId,
				SupplyTypeId = x.si.SupplyTypeId,
				SupplyConditionId = x.sc != null ? x.sc.Id : (Guid?)null,
				ExpectedDate = x.si.ExpectedDate,
				Quantity = x.si.Quantity,
				CommentId = x.si.CommentId,
				Comment = c != null ? c.Content : null
			})
			.OrderBy(x => x.SupplyTypeId)
			.ToListAsync();
	}

	public async Task<OperationResultDto> UpdateSupplyItemAsync(
			Guid orderId,
			Guid supplyTypeId,
			UpdateSupplyItemRequest request)
	{
		var orderSupply = await _context.OrderSupplies
			.FirstOrDefaultAsync(os => os.OrderId == orderId);

		if (orderSupply == null)
			return new OperationResultDto { Success = false, Message = "Order supply not found" };

		var supplyItem = await _context.SupplyItems
			.FirstOrDefaultAsync(si => si.OrderSupplyId == orderSupply.Id && si.SupplyTypeId == supplyTypeId);

		if (supplyItem == null)
			return new OperationResultDto { Success = false, Message = "Supply item not found" };

		supplyItem.ConditionId = request.SupplyConditionId;
		supplyItem.ExpectedDate = request.ExpectedDate;
		supplyItem.Quantity = request.Quantity;
		supplyItem.Comment = request.Comment;
		supplyItem.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		// Оборачиваем уведомления в try-catch, чтобы тесты с InMemory не падали
		//try
		//{
		//	await NotificationHelper.SupplyUpdated(orderId, supplyTypeId, supplyItem.ConditionId);
		//}
		//catch (Exception ex)
		//{
		//	_logger.LogWarning(ex, "Failed to send supply notification (likely running in tests)");
		//}

		return new OperationResultDto { Success = true, Message = "Supply item updated" };
	}

	public async Task<OperationResultDto> UpdateAllSupplyItemsAsync(
		Guid orderId,
		List<UpdateSupplyItemRequest> updates)
	{
		var orderSupply = await _context.OrderSupplies
			.FirstOrDefaultAsync(os => os.OrderId == orderId);

		if (orderSupply == null)
			return new OperationResultDto { Success = false, Message = "Order supply not found" };

		foreach (var update in updates)
		{
			var supplyItem = await _context.SupplyItems
				.FirstOrDefaultAsync(si => si.OrderSupplyId == orderSupply.Id && si.SupplyTypeId == update.SupplyTypeId);

			if (supplyItem != null)
			{
				// Если есть комментарий и он изменился
				if (!string.IsNullOrEmpty(update.Comment))
				{
					if (supplyItem.CommentId.HasValue)
					{
						// Обновляем существующий комментарий
						var existingComment = await _context.Comments
							.FirstOrDefaultAsync(c => c.Id == supplyItem.CommentId.Value);
						if (existingComment != null)
						{
							existingComment.Content = update.Comment;
							existingComment.UpdatedAt = DateTime.UtcNow;
						}
					}
					else
					{
						// Создаём новый комментарий
						var newComment = new Comment
						{
							Id = Guid.NewGuid(),
							OrderId = orderId,
							UserId = update.UserId,
							Content = update.Comment,
							CreatedAt = DateTime.UtcNow,
							UpdatedAt = DateTime.UtcNow
						};
						_context.Comments.Add(newComment);
						await _context.SaveChangesAsync();

						supplyItem.CommentId = newComment.Id;
					}
				}

				// Обновляем остальные поля
				supplyItem.ConditionId = update.SupplyConditionId;
				supplyItem.ExpectedDate = update.ExpectedDate;
				supplyItem.Quantity = update.Quantity;
				supplyItem.UpdatedAt = DateTime.UtcNow;
			}
		}

		await _context.SaveChangesAsync();

		return new OperationResultDto
		{
			Success = true,
			Message = $"{updates.Count} supply items updated"
		};
	}
	
	public async Task<PaginatedResponse<SupplyStatusListItemDto>> GetAllSupplyItemsAsync(
		int page, int limit, string? orderNumber)
	{
		var query = _context.SupplyItems
			.Join(_context.OrderSupplies, si => si.OrderSupplyId, os => os.Id, (si, os) => new { si, os })
			.Join(_context.Orders, x => x.os.OrderId, o => o.Id, (x, o) => new { x.si, x.os, o })
			.Join(_context.SupplyTypes, x => x.si.SupplyTypeId, st => st.Id, (x, st) => new { x.si, x.os, x.o, st })
			.GroupBy(x => new { x.o.Id, x.o.OrderNumber, x.o.ReadyDate })
			.Select(g => new SupplyStatusListItemDto
			{
				Id = g.Key.Id,
				OrderNumber = g.Key.OrderNumber,
				ReadyDate = g.Key.ReadyDate,
				Lumber = g.FirstOrDefault(x => x.st.Name == "lumber")!.si.ConditionId == null ? null :
					_context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "lumber").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				Paint = g.FirstOrDefault(x => x.st.Name == "paint")!.si.ConditionId == null ? null :
					_context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "paint").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				Glass = g.FirstOrDefault(x => x.st.Name == "glass")!.si.ConditionId == null ? null :
					_context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "glass").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				Furniture = g.FirstOrDefault(x => x.st.Name == "furniture")!.si.ConditionId == null ? null :
					_context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "furniture").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				AlumWaterShield = g.FirstOrDefault(x => x.st.Name == "alumWaterShield")!.si.ConditionId == null ? null :
					_context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "alumWaterShield").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				// Комментарии
				LumberComment = g.Where(x => x.st.Name == "lumber")
								 .Select(x => x.si.CommentEntity != null ? x.si.CommentEntity.Content : null)
								 .FirstOrDefault(),
				PaintComment = g.Where(x => x.st.Name == "paint")
								.Select(x => x.si.CommentEntity != null ? x.si.CommentEntity.Content : null)
								.FirstOrDefault(),
				GlassComment = g.Where(x => x.st.Name == "glass")
								.Select(x => x.si.CommentEntity != null ? x.si.CommentEntity.Content : null)
								.FirstOrDefault(),
				FurnitureComment = g.Where(x => x.st.Name == "furniture")
									.Select(x => x.si.CommentEntity != null ? x.si.CommentEntity.Content : null)
									.FirstOrDefault(),
				AlumWaterShieldComment = g.Where(x => x.st.Name == "alumWaterShield")
										  .Select(x => x.si.CommentEntity != null ? x.si.CommentEntity.Content : null)
										  .FirstOrDefault()

			});

		if (!string.IsNullOrEmpty(orderNumber))
			query = query.Where(s => EF.Functions.ILike(s.OrderNumber, $"%{orderNumber}%"));

		var total = await query.CountAsync();

		var items = await query
			.Skip((page - 1) * limit)
			.Take(limit)
			.ToListAsync();

		return new PaginatedResponse<SupplyStatusListItemDto>
		{
			Data = items,
			Pagination = new PaginationInfo
			{
				Page = page,
				Limit = limit,
				Total = total,
				Pages = (int)Math.Ceiling(total / (double)limit)
			}
		};
	}
}