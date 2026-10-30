using KG.MES.Server.Extensions;
using KG.MES.Shared.Common.Extensions;
using KG.MES.Shared.Model.Enums;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Shared.Services;

public partial class OrderService
{
	public async Task<OrderCommercialDto?> GetOrderCommercialAsync(Guid orderId)
	{
		var commercial = await context.OrderCommercials
			.Include(oc => oc.Manager)
			.Include(oc => oc.Customer)
			.FirstOrDefaultAsync(oc => oc.OrderId == orderId);

		if (commercial == null)
			return null;

		return new OrderCommercialDto
		{
			ManagerId = commercial.ManagerId,
			ManagerName = commercial.Manager?.Name,
			CustomerId = commercial.CustomerId,
			CustomerName = commercial.CustomerName ?? commercial.Customer?.Name,
			Amount = commercial.Amount,
			Currency = commercial.Currency
		};
	}

	// Основной метод GetOrdersAsync
	public async Task<PaginatedResponse<SalesOrderDto>> GetSalesOrdersAsync(
		int page, int limit, string? sortBy, string? sortOrder,
		List<Guid>? workplaceIds, string? orderNumber,
		string? customerName, Guid? managerId,
		Guid? currentUserId, Guid? currentRoleId) // <-- Добавляем для уведомлений
	{
		var query = BuildSalesQuery();

		// 1. Фильтры
		if (!string.IsNullOrEmpty(orderNumber))
			query = query.Where(o => EF.Functions.ILike(o.OrderNumber, $"%{orderNumber}%"));

		if (workplaceIds != null && workplaceIds.Any())
			query = query.Where(o => workplaceIds.Contains(o.CurrentWorkplaceId ?? Guid.Empty));

		if (!string.IsNullOrEmpty(customerName))
			query = query.Where(o => o.CustomerName != null && EF.Functions.ILike(o.CustomerName, $"%{customerName}%"));

		if (managerId.HasValue)
			query = query.Where(o => o.ManagerId == managerId); // Если есть ManagerId в DTO

		// 2. Сортировка
		var orderedQuery = query.OrderByProperty(sortBy ?? "ReadyDate", sortOrder);

		// 3. Пагинация
		var total = await orderedQuery.CountAsync();
		var items = await orderedQuery
			.Skip((page - 1) * limit)
			.Take(limit)
			.ToListAsync();

		// 4. Пометка новых (если нужно)
		if (currentUserId.HasValue && currentRoleId.HasValue)
		{
			var newOrderIds = await notificationService.GetUnreadOrderIdsAsync(
				currentUserId.Value, currentRoleId, NotificationEventType.OrderReady);

			var newSet = newOrderIds.ToHashSet();
			foreach (var item in items)
			{
				item.IsNew = newSet.Contains(item.Id);
			}
		}

		return new PaginatedResponse<SalesOrderDto>
		{
			Data = items,
			Pagination = new PaginationInfo
			{
				Page = page,
				Limit = limit,
				Total = total,
				Pages = (int)Math.Ceiling(total / (double)limit)
			},
			Sort = new SortInfo { By = sortBy ?? "ReadyDate", Order = sortOrder ?? "asc" }
		};
	}

	public async Task<List<NotificationGroupDto<SalesOrderDto>>> GetNewSalesOrdersAsync(
		Guid userId, Guid? roleId)
	{
		var notifications = await notificationService.GetUnreadOrderNotificationsAsync(userId, roleId);

		if (!notifications.Any())
			return new List<NotificationGroupDto<SalesOrderDto>>();

		var orderIds = notifications.Select(n => n.OrderId).Distinct().ToList();
		var orders = await BuildSalesQuery().Where(o => orderIds.Contains(o.Id)).ToListAsync();
		var orderDict = orders.ToDictionary(o => o.Id);

		return notifications
			.Where(n => orderDict.ContainsKey(n.OrderId))
			.GroupBy(n => new { n.EventCode, n.EventDescription, n.Severity, severityOrder = n.GetSeverityOrder() })
			.OrderBy(g => g.Key.severityOrder)
			.Select(g => new NotificationGroupDto<SalesOrderDto>
			{
				EventCode = g.Key.EventCode,
				EventDescription = g.Key.EventDescription,
				Severity = g.Key.Severity,
				Items = g.Select(n => orderDict[n.OrderId]).ToList()
			})
			.ToList();
	}

	/// <summary>
	/// Создать или обновить коммерческую информацию по заказу
	/// </summary>
	public async Task<OrderCommercialDto> UpdateOrderCommercialAsync(Guid orderId, OrderCommercialRequestDto dto)
	{
		// Проверяем существование заказа
		var orderExists = await context.Orders.AnyAsync(o => o.Id == orderId);
		if (!orderExists)
			throw new Exception($"Заказ {orderId} не найден");

		// Ищем существующую запись
		var existing = await context.OrderCommercials
			.FirstOrDefaultAsync(oc => oc.OrderId == orderId);

		// Если есть контрагент по ID — проверим его существование
		if (dto.CustomerId.HasValue)
		{
			var customerExists = await context.Customers.AnyAsync(c => c.Id == dto.CustomerId.Value);
			if (!customerExists)
				throw new Exception($"Контрагент {dto.CustomerId} не найден");
		}

		if (existing == null)
		{
			// Создаём новую запись
			var commercial = new OrderCommercial
			{
				Id = Guid.NewGuid(),
				OrderId = orderId,
				ManagerId = dto.ManagerId,
				CustomerId = dto.CustomerId,
				CustomerName = dto.CustomerName,
				Amount = dto.Amount,
				Currency = dto.Currency ?? "RUB",
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			context.OrderCommercials.Add(commercial);
			await context.SaveChangesAsync();

			// Загружаем менеджера и контрагента для ответа
			await context.Entry(commercial).Reference(oc => oc.Manager).LoadAsync();
			await context.Entry(commercial).Reference(oc => oc.Customer).LoadAsync();

			return new OrderCommercialDto
			{
				ManagerId = commercial.ManagerId,
				ManagerName = commercial.Manager?.Name,
				CustomerId = commercial.CustomerId,
				CustomerName = commercial.CustomerName ?? commercial.Customer?.Name,
				Amount = commercial.Amount,
				Currency = commercial.Currency
			};
		}
		else
		{
			// Обновляем существующую запись
			existing.ManagerId = dto.ManagerId;
			existing.CustomerId = dto.CustomerId;
			existing.CustomerName = dto.CustomerName;
			existing.Amount = dto.Amount;
			existing.Currency = dto.Currency ?? existing.Currency;
			existing.UpdatedAt = DateTime.UtcNow;

			await context.SaveChangesAsync();

			// Загружаем менеджера и контрагента для ответа
			await context.Entry(existing).Reference(oc => oc.Manager).LoadAsync();
			await context.Entry(existing).Reference(oc => oc.Customer).LoadAsync();

			return new OrderCommercialDto
			{
				ManagerId = existing.ManagerId,
				ManagerName = existing.Manager?.Name,
				CustomerId = existing.CustomerId,
				CustomerName = existing.CustomerName ?? existing.Customer?.Name,
				Amount = existing.Amount,
				Currency = existing.Currency
			};
		}
	}

	/// <summary>
	/// Получить список контрагентов (для автодополнения)
	/// </summary>
	public async Task<List<CustomerDto>> GetCustomersAsync(string? search = null)
	{
		var query = context.Customers.AsQueryable();

		if (!string.IsNullOrEmpty(search))
		{
			query = query.Where(c =>
				EF.Functions.ILike(c.Name, $"%{search}%") ||
				EF.Functions.ILike(c.Inn ?? "", $"%{search}%") ||
				EF.Functions.ILike(c.Phone ?? "", $"%{search}%")
			);
		}

		return await query
			.OrderBy(c => c.Name)
			.Select(c => new CustomerDto
			{
				Id = c.Id,
				Name = c.Name,
				Inn = c.Inn,
				Phone = c.Phone,
				Email = c.Email,
				Address = c.Address
			})
			.Take(50)  // ограничиваем количество
			.ToListAsync();
	}

	/// <summary>
	/// Базовый запрос для списка продаж (без фильтров и пагинации)
	/// </summary>
	private IQueryable<SalesOrderDto> BuildSalesQuery()
	{
		return from o in context.Orders
			join po in context.ProductionOrders on o.Id equals po.OrderId into poGroup
			from po in poGroup.DefaultIfEmpty()
			join w in context.Workplaces on po.CurrentWorkplaceId equals w.Id into wGroup
			from w in wGroup.DefaultIfEmpty()
			join oc in context.OrderCommercials on o.Id equals oc.OrderId into ocGroup
			from oc in ocGroup.DefaultIfEmpty()
			join u in context.Users on oc.ManagerId equals u.Id into uGroup
			from u in uGroup.DefaultIfEmpty()
			join c in context.Customers on oc.CustomerId equals c.Id into cGroup
			from c in cGroup.DefaultIfEmpty()
			select new SalesOrderDto
			{
				Id = o.Id,
				OrderNumber = o.OrderNumber,
				ReadyDate = o.ReadyDate,
				WindowCount = o.WindowCount,
				WindowArea = o.WindowArea,
				PlateCount = o.PlateCount,
				PlateArea = o.PlateArea,
				IsEconom = o.IsEconom,
				IsClaim = o.IsClaim,
				IsOnlyPaid = o.IsOnlyPaid,
				CreatedAt = o.CreatedAt,
				ProductionOrderId = po != null ? po.Id : (Guid?)null,
				CurrentWorkplaceId = po != null ? po.CurrentWorkplaceId : (Guid?)null,
				CurrentStatus = w != null ? w.Name : null,
				CustomerName = c != null ? c.Name : oc != null ? oc.CustomerName : null,
				ManagerName = u != null ? u.Name : null,
				Amount = oc != null ? oc.Amount : null,
				Currency = oc != null ? oc.Currency : null
			};
	}
}