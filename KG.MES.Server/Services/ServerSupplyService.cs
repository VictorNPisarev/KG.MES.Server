// KG.MES.Server/Services/SupplyService.cs
using KG.MES.Server.Data;
using KG.MES.Shared.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using Microsoft.EntityFrameworkCore;
using KG.MES.Shared.Models.Entities;
using KG.MES.Server.Extensions;
using KG.MES.Server.Services;
using KG.MES.Shared.Model.Enums;

namespace KG.MES.Shared.Services;

public class ServerSupplyService : ISupplyService
{
	private readonly AppDbContext context;
	private readonly ILogger<ServerSupplyService> logger;

	private readonly NotificationService notificationService;


	public ServerSupplyService(AppDbContext context, ILogger<ServerSupplyService> logger, NotificationService notificationService)
	{
		this.context = context;
		this.logger = logger;
		this.notificationService = notificationService;
	}

	public async Task<List<SupplyTypeDto>> GetSupplyTypesAsync()
	{
		return await context.SupplyTypes
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
		return await context.SupplyConditions
			.OrderBy(sc => sc.SortOrder)
			.Select(sc => new SupplyConditionDto
			{
				Id = sc.Id,
				ConditionCode = sc.ConditionCode,
				SortOrder = sc.SortOrder
			})
			.ToListAsync();
	}

	public async Task<List<OrderSupplyDto>> GetOrderSupplyItemsAsync(Guid orderId)
	{
		return await context.OrderSupplies
			.Where(os => os.OrderId == orderId)
			.SelectMany(os => os.SupplyItems!)
			.Join(context.SupplyTypes, si => si.SupplyTypeId, st => st.Id, (si, st) => new { si, st })
			.GroupJoin(context.SupplyConditions, x => x.si.ConditionId, sc => sc.Id, (x, sc) => new { x.si, x.st, sc })
			.SelectMany(x => x.sc.DefaultIfEmpty(), (x, sc) => new { x.si, x.st, sc })
			.GroupJoin(context.Comments, x => x.si.CommentId, c => c.Id, (x, c) => new { x.si, x.st, x.sc, c })
			.SelectMany(x => x.c.DefaultIfEmpty(), (x, c) => new OrderSupplyDto
			{
				OrderSupplyId = x.si.OrderSupplyId,
				SupplyItemId = x.si.Id,
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
		var orderSupply = await context.OrderSupplies
			.FirstOrDefaultAsync(os => os.OrderId == orderId);

		if (orderSupply == null)
			return new OperationResultDto { Success = false, Message = "Order supply not found" };

		var supplyItem = await context.SupplyItems
			.FirstOrDefaultAsync(si => si.OrderSupplyId == orderSupply.Id && si.SupplyTypeId == supplyTypeId);

		if (supplyItem == null)
			return new OperationResultDto { Success = false, Message = "Supply item not found" };

		supplyItem.ConditionId = request.SupplyConditionId;
		supplyItem.ExpectedDate = request.ExpectedDate;
		supplyItem.Quantity = request.Quantity;
		supplyItem.Comment = request.Comment;
		supplyItem.UpdatedAt = DateTime.UtcNow;

		await context.SaveChangesAsync();

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
		var orderSupply = await context.OrderSupplies
			.FirstOrDefaultAsync(os => os.OrderId == orderId);

		if (orderSupply == null)
			return new OperationResultDto { Success = false, Message = "Order supply not found" };

		foreach (var update in updates)
		{
			var supplyItem = await context.SupplyItems
				.FirstOrDefaultAsync(si => si.OrderSupplyId == orderSupply.Id
											&& si.SupplyTypeId == update.SupplyTypeId);

			if (supplyItem == null) continue;

			// ✅ Логируем ДО обновления
			await LogSupplyChangeAsync(
				supplyItem.Id,
				update.UserId,
				update.SupplyConditionId,
				update.Comment,
				source: "API");

			// Обновляем комментарий
			if (!string.IsNullOrEmpty(update.Comment))
			{
				if (supplyItem.CommentId.HasValue)
				{
					var existingComment = await context.Comments
						.FirstOrDefaultAsync(c => c.Id == supplyItem.CommentId.Value);
					if (existingComment != null)
					{
						existingComment.Content = update.Comment;
						existingComment.UpdatedAt = DateTime.UtcNow;
						existingComment.UserId = update.UserId;
					}
				}
				else
				{
					var newComment = new Comment
					{
						Id = Guid.NewGuid(),
						OrderId = orderId,
						UserId = update.UserId,
						Content = update.Comment,
						CreatedAt = DateTime.UtcNow,
						UpdatedAt = DateTime.UtcNow
					};
					context.Comments.Add(newComment);
					supplyItem.CommentId = newComment.Id;
				}
			}

			// Обновляем остальные поля
			supplyItem.ConditionId = update.SupplyConditionId;
			supplyItem.ExpectedDate = update.ExpectedDate;
			supplyItem.Quantity = update.Quantity;
			supplyItem.UpdatedAt = DateTime.UtcNow;
		}

		await context.SaveChangesAsync();

		return new OperationResultDto
		{
			Success = true,
			Message = $"{updates.Count} supply items updated"
		};
	}
	
	public async Task<PaginatedResponse<SupplyDto>> GetAllSupplyItemsAsync(
		int page, int limit, string? sortBy, string? sortOrder, List<Guid>? workplaceIds, string? orderNumber)
	{
		var query = BuildSupplyQuery(workplaceIds, orderNumber);

		var total = await query.CountAsync();

		var items = await query
			.OrderBy(o => o.ReadyDate)
			.Skip((page - 1) * limit)
			.Take(limit)
			.ToListAsync();

		return new PaginatedResponse<SupplyDto>
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

	/// <summary>
	/// Логирует изменение снабжения (только если что-то изменилось)
	/// </summary>
	private async Task LogSupplyChangeAsync(
		Guid supplyItemId,
		Guid? userId,
		Guid? conditionId,
		string? comment,
		string source = "API")
	{
		// Получаем последний лог
		var lastLog = await context.SupplyLogs
			.Where(sl => sl.SupplyItemId == supplyItemId)
			.OrderByDescending(sl => sl.OperationTime)
			.FirstOrDefaultAsync();

		// Проверяем, изменилось ли что-то
		var conditionChanged = lastLog?.ConditionId != conditionId;
		var commentChanged = lastLog?.Comment != comment;

		if (!conditionChanged && !commentChanged)
			return;

		context.SupplyLogs.Add(new SupplyLog
		{
			Id = Guid.NewGuid(),
			SupplyItemId = supplyItemId,
			UserId = userId,
			ConditionId = conditionId,
			Comment = comment,
			OperationTime = DateTime.UtcNow,
			Source = source
		});
	}

	public async Task<List<SupplyItemHistoryDto>> GetSupplyHistoryAsync(Guid supplyItemId)
	{
		var logs = await context.SupplyLogs
			.Where(sl => sl.SupplyItemId == supplyItemId)
			.OrderByDescending(sl => sl.OperationTime)
			.Select(sl => new SupplyItemHistoryDto
			{
				Id = sl.Id,
				SupplyTypeName = sl.SupplyItem.SupplyType.Name,
				ConditionCode = sl.Condition != null ? sl.Condition.ConditionCode : null,
				Comment = sl.Comment,
				UserId = sl.UserId,
				UserName = sl.User != null ? sl.User.Name : null,
				OperationTime = sl.OperationTime,
				Source = sl.Source
			})
			.ToListAsync();

		foreach (var log in logs)
			log.OperationTime = log.OperationTime.ToProductionTime();

		return logs;
	}

	private IQueryable<SupplyDto> BuildSupplyQuery(List<Guid>? workplaceIds = null, string? orderNumber = null)
	{
		var orderQuery = context.Orders.Where(o => !o.IsOnlyPaid).AsQueryable();

		if (!string.IsNullOrEmpty(orderNumber))
			orderQuery = orderQuery.Where(o => EF.Functions.ILike(o.OrderNumber, $"%{orderNumber}%"));

		var wProductionOrderQuery = orderQuery.Join(context.ProductionOrders, o => o.Id, po => po.OrderId, (o, po) => new { o, po });

		if (workplaceIds != null && workplaceIds.Any())
			wProductionOrderQuery = wProductionOrderQuery.Where(o => workplaceIds.Contains(o.po.CurrentWorkplaceId ?? Guid.Empty));

		return wProductionOrderQuery
			.Join(context.OrderSupplies, x => x.o.Id, os => os.OrderId, (x, os) => new { x.o, x.po, os })
			.Join(context.SupplyItems, x => x.os.Id, si => si.OrderSupplyId, (x, si) => new { x.o, x.po, x.os, si })
			.Join(context.SupplyTypes, x => x.si.SupplyTypeId, st => st.Id, (x, st) => new { x.si, x.os, x.o, x.po, st })
			.Join(context.Workplaces, x => x.po.CurrentWorkplaceId, w => w.Id, (x, w) => new { x.o, x.po, x.os, x.si, x.st, w })
			.GroupBy(x => new
			{
				x.o.Id,
				x.o.OrderNumber,
				x.o.ReadyDate,
				x.o.RtmDate,
				x.o.IsClaim,
				x.o.IsEconom,
				x.o.IsOnlyPaid,
				x.po.Machine,
				ProductionOrderId = x.po.Id,
				x.po.CurrentWorkplaceId,
				x.po.IsTwoSidePaint,
				WorkplaceName = x.w.Name
			})
			.Select(g => new SupplyDto
			{
				Id = g.Key.Id,
				OrderNumber = g.Key.OrderNumber,
				ReadyDate = g.Key.ReadyDate,
				RtmDate = g.Key.RtmDate,
				Machine = g.Key.Machine,
				ProductionOrderId = g.Key.ProductionOrderId,
				CurrentWorkplaceId = g.Key.CurrentWorkplaceId,
				Status = g.Key.WorkplaceName,
				IsClaim = g.Key.IsClaim,
				IsEconom = g.Key.IsEconom,
				IsOnlyPaid = g.Key.IsOnlyPaid,
				IsTwoSidePaint = g.Key.IsTwoSidePaint,
				Lumber = g.FirstOrDefault(x => x.st.Name == "lumber")!.si.ConditionId == null ? null :
					context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "lumber").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				Paint = g.FirstOrDefault(x => x.st.Name == "paint")!.si.ConditionId == null ? null :
					context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "paint").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				Glass = g.FirstOrDefault(x => x.st.Name == "glass")!.si.ConditionId == null ? null :
					context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "glass").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				Furniture = g.FirstOrDefault(x => x.st.Name == "furniture")!.si.ConditionId == null ? null :
					context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "furniture").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				AlumWaterShield = g.FirstOrDefault(x => x.st.Name == "alumWaterShield")!.si.ConditionId == null ? null :
					context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "alumWaterShield").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				Windowsill = g.FirstOrDefault(x => x.st.Name == "windowsill")!.si.ConditionId == null ? null :
					context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "windowsill").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
				WoodAlum = g.FirstOrDefault(x => x.st.Name == "woodAlum")!.si.ConditionId == null ? null :
					context.SupplyConditions.Where(sc => sc.Id == g.First(x => x.st.Name == "woodAlum").si.ConditionId).Select(sc => sc.ConditionCode).FirstOrDefault(),
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
										  .FirstOrDefault(),
				WindowsillComment = g.Where(x => x.st.Name == "windowsill")
										  .Select(x => x.si.CommentEntity != null ? x.si.CommentEntity.Content : null)
										  .FirstOrDefault(),
				WoodAlumComment = g.Where(x => x.st.Name == "woodAlum")
										  .Select(x => x.si.CommentEntity != null ? x.si.CommentEntity.Content : null)
										  .FirstOrDefault()
			});
	}

	public async Task<List<SupplyDto>> GetNewSupplyOrdersAsync(Guid userId, Guid? roleId)
	{
		// 1. Получаем ID новых заказов (событие NEW_ORDER)
		var newOrderIds = await notificationService.GetUnreadOrderIdsAsync(userId, roleId, NotificationEventType.NewOrder);

		if (!newOrderIds.Any()) return new List<SupplyDto>();

		// 2. Возвращаем их в формате снабжения
		return await BuildSupplyQuery() // твой базовый запрос для снабжения
			.Where(x => newOrderIds.Contains(x.Id))
			.OrderByDescending(x => x.ReadyDate) // или по дате создания уведомления
			.ToListAsync();
	}
}