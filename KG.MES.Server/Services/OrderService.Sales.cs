using KG.MES.Server.Extensions;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public partial class OrderService
{
	public async Task<OrderCommercialDto?> GetOrderCommercialAsync(Guid orderId)
	{
		var commercial = await _context.OrderCommercials
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
	public async Task<PaginatedResponse<SalesOrderListItemDto>> GetSalesOrdersAsync(
		int page, int limit, string? sortBy, string? sortOrder, List<Guid>? workplaceIds, string? orderNumber)
	{
		var orderQuery = _context.Orders.AsQueryable();

		if (!string.IsNullOrEmpty(orderNumber))
			orderQuery = orderQuery.Where(o => EF.Functions.ILike(o.OrderNumber, $"%{orderNumber}%"));

		var wProductionOrderQuery = orderQuery.Join(_context.ProductionOrders, o => o.Id, po => po.OrderId, (o, po) => new { o, po });

		if (
			//string.IsNullOrEmpty(orderNumber) && 
			workplaceIds != null && workplaceIds.Any())
		{
			wProductionOrderQuery = wProductionOrderQuery.Where(o => workplaceIds.Contains(o.po.CurrentWorkplaceId ?? Guid.Empty));
		}

		var query = wProductionOrderQuery
			.Join(_context.Workplaces, x => x.po.CurrentWorkplaceId, w => w.Id, (x, w) => new { x.o, x.po, w })
			.LeftJoin(_context.OrderCommercials, x => x.o.Id, s => s.OrderId, (x, s) => new {x.o, x.po, x.w, s})
			.LeftJoin(_context.Users, x => x.s != null ? x.s.ManagerId : (Guid?)null, m => m.Id, (x, m) => new {x.o, x.po, x.w, x.s, m})
			.LeftJoin(_context.Customers, x => x.s != null ? x.s.CustomerId : (Guid?)null, c => c.Id, (x, c) => 
			new SalesOrderListItemDto
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
				ProductionOrderId = x.po != null ? x.po.Id : (Guid?)null,
				CurrentWorkplaceId = x.po != null ? x.po.CurrentWorkplaceId : (Guid?)null,
				CurrentStatus = x.w != null ? x.w.Name : null,
				CustomerName = c != null ? c.Name : null,
				ManagerName = x.m != null ? x.m.Name : null,
				Amount = x.s != null ? x.s.Amount : null,
				Currency = x.s != null ? x.s.Currency : null
			});

		var total = await query.CountAsync();

		// Применяем сортировку
		var orderedQuery = query.OrderBy(o => o.ReadyDate);

		//switch (sortBy?.ToLower())
		//{
		//	case "order_number":
		//		orderedQuery = OrderByOrderNumber(query, sortOrder);
		//		break;
		//	case "window_count":
		//		orderedQuery = OrderByWindowCount(query, sortOrder);
		//		break;
		//	case "plate_count":
		//		orderedQuery = OrderByPlateCount(query, sortOrder);
		//		break;
		//	case "ready_date":
		//	default:
				//orderedQuery = OrderByReadyDate(query, sortOrder);
		//		break;
		//}

		var items = await orderedQuery
			.Skip((page - 1) * limit)
			.Take(limit)
			.ToListAsync();

		return new PaginatedResponse<SalesOrderListItemDto>
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

	/// <summary>
	/// Получить список заказов с коммерческой информацией (для отдела продаж)
	/// </summary>
	public async Task<PaginatedResponse<SalesOrderListItemDto>> GetSalesOrdersAsync(
		int page,
		int limit,
		string? sortBy,
		string? sortOrder,
		string? orderNumber,
		string? customerName,
		Guid? managerId)
	{
		var query = from o in _context.Orders
					join po in _context.ProductionOrders on o.Id equals po.OrderId into poGroup
					from po in poGroup.DefaultIfEmpty()
					join w in _context.Workplaces on po.CurrentWorkplaceId equals w.Id into wGroup
					from w in wGroup.DefaultIfEmpty()
					join oc in _context.OrderCommercials on o.Id equals oc.OrderId into ocGroup
					from oc in ocGroup.DefaultIfEmpty()
					join u in _context.Users on oc.ManagerId equals u.Id into uGroup
					from u in uGroup.DefaultIfEmpty()
					select new SalesOrderListItemDto
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
						CustomerName = oc != null ? oc.CustomerName : null,
						ManagerName = u != null ? u.Name : null,
						Amount = oc != null ? oc.Amount : null,
						Currency = oc != null ? oc.Currency : null
					};

		// Фильтры
		if (!string.IsNullOrEmpty(orderNumber))
			query = query.Where(o => EF.Functions.ILike(o.OrderNumber, $"%{orderNumber}%"));

		if (!string.IsNullOrEmpty(customerName))
			query = query.Where(o => EF.Functions.ILike(o.CustomerName ?? "", $"%{customerName}%"));

		if (managerId.HasValue)
			query = query.Where(o => o.ManagerName != null); // или сравнивать с ID

		var total = await query.CountAsync();

		// Сортировка
		IOrderedQueryable<SalesOrderListItemDto> orderedQuery;

		switch (sortBy?.ToLower())
		{
			case "order_number":
				orderedQuery = sortOrder == "desc"
					? query.OrderByDescending(o => o.OrderNumber)
					: query.OrderBy(o => o.OrderNumber);
				break;
			case "customer_name":
				orderedQuery = sortOrder == "desc"
					? query.OrderByDescending(o => o.CustomerName)
					: query.OrderBy(o => o.CustomerName);
				break;
			case "amount":
				orderedQuery = sortOrder == "desc"
					? query.OrderByDescending(o => o.Amount)
					: query.OrderBy(o => o.Amount);
				break;
			case "ready_date":
			default:
				orderedQuery = sortOrder == "desc"
					? query.OrderByDescending(o => o.ReadyDate)
					: query.OrderBy(o => o.ReadyDate);
				break;
		}

		var items = await orderedQuery
			.Skip((page - 1) * limit)
			.Take(limit)
			.ToListAsync();

		return new PaginatedResponse<SalesOrderListItemDto>
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
			},
			Filters = new FilterInfo
			{
				OrderNumber = orderNumber
			}
		};
	}

	/// <summary>
	/// Создать или обновить коммерческую информацию по заказу
	/// </summary>
	public async Task<OrderCommercialDto> UpdateOrderCommercialAsync(Guid orderId, OrderCommercialRequestDto dto)
	{
		// Проверяем существование заказа
		var orderExists = await _context.Orders.AnyAsync(o => o.Id == orderId);
		if (!orderExists)
			throw new Exception($"Заказ {orderId} не найден");

		// Ищем существующую запись
		var existing = await _context.OrderCommercials
			.FirstOrDefaultAsync(oc => oc.OrderId == orderId);

		// Если есть контрагент по ID — проверим его существование
		if (dto.CustomerId.HasValue)
		{
			var customerExists = await _context.Customers.AnyAsync(c => c.Id == dto.CustomerId.Value);
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

			_context.OrderCommercials.Add(commercial);
			await _context.SaveChangesAsync();

			// Загружаем менеджера и контрагента для ответа
			await _context.Entry(commercial).Reference(oc => oc.Manager).LoadAsync();
			await _context.Entry(commercial).Reference(oc => oc.Customer).LoadAsync();

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

			await _context.SaveChangesAsync();

			// Загружаем менеджера и контрагента для ответа
			await _context.Entry(existing).Reference(oc => oc.Manager).LoadAsync();
			await _context.Entry(existing).Reference(oc => oc.Customer).LoadAsync();

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
		var query = _context.Customers.AsQueryable();

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
}