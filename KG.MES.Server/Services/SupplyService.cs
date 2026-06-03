// KG.MES.Server/Services/SupplyService.cs
using KG.MES.Server.Data;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using KG.MES.Server.Hubs;

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
			.OrderBy(st => st.SortOrder)
			.Select(st => new SupplyTypeDto
			{
				Id = st.Id,
				Name = st.Name,
				DisplayName = st.DisplayName,
				Unit = st.Unit,
				SortOrder = st.SortOrder,
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
				DisplayName = sc.DisplayName,
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
			.Select(x => new OrderSupplyItemDto
			{
				OrderSupplyId = x.si.OrderSupplyId,
				SupplyTypeId = x.si.SupplyTypeId,
				SupplyTypeName = x.st.Name,
				DisplayName = x.st.DisplayName,
				Unit = x.st.Unit,
				SupplyConditionId = x.si.ConditionId,
				ExpectedDate = x.si.ExpectedDate,
				Quantity = x.si.Quantity,
				Comment = x.si.Comment
			})
			.ToListAsync();
	}

	public async Task<OperationResultDto> UpdateSupplyItemAsync(
		Guid orderId, Guid supplyTypeId, UpdateSupplyItemRequest request)
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

		await NotificationHelper.SupplyStatusChanged(orderId, supplyTypeId, request.SupplyConditionId);
		await NotificationHelper.SupplyUpdated(orderId, supplyTypeId, request.SupplyConditionId);
		
		return new OperationResultDto { Success = true, Message = "Supply item updated" };
	}

	public async Task<OperationResultDto> UpdateAllSupplyItemsAsync(
		Guid orderId, List<UpdateSupplyItemRequest> updates)
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
				supplyItem.ConditionId = update.SupplyConditionId;
				supplyItem.ExpectedDate = update.ExpectedDate;
				supplyItem.Quantity = update.Quantity;
				supplyItem.Comment = update.Comment;
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
				OrderId = g.Key.Id,
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
					_context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "alumWaterShield").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault()
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