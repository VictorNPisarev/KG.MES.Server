// KG.MES.Server/Services/OrderService.Comments.cs
using KG.MES.Server.Hubs;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public partial class OrderService
{
	public async Task<List<OrderCommentDto>> GetOrderCommentsAsync(Guid orderId)
	{
		var commentIds = await _context.Orders
			.Where(o => o.Id == orderId)
			.Select(o => o.CommentIds ?? new List<Guid>())
			.FirstOrDefaultAsync() ?? new List<Guid>();

		if (commentIds.Count == 0)
			return new List<OrderCommentDto>();

		var comments = await _context.Comments
			.Where(c => commentIds.Contains(c.Id))
			.Join(_context.Users, c => c.UserId, u => u.Id, (c, u) => new OrderCommentDto
			{
				Id = c.Id,
				Content = c.Content,
				CreatedAt = c.CreatedAt,
				UpdatedAt = c.UpdatedAt,
				UserId = c.UserId,
				UserName = u.Name
			})
			.OrderByDescending(c => c.CreatedAt)
			.ToListAsync();

		return comments;
	}

	public async Task<OrderCommentDto> AddOrderCommentAsync(Guid orderId, Guid? userId, string content)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var comment = new Comment
			{
				Id = Guid.NewGuid(),
				OrderId = orderId,
				UserId = userId,
				Content = content,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_context.Comments.Add(comment);
			await _context.SaveChangesAsync();

			var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
			if (order != null)
			{
				order.CommentIds = (order.CommentIds ?? new List<Guid>()).Append(comment.Id).ToList();
				order.UpdatedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}

			await transaction.CommitAsync();

			await NotificationHelper.OrderCommentAdded(orderId, comment.Id, content, userId);

			return new OrderCommentDto
			{
				Id = comment.Id,
				Content = comment.Content,
				CreatedAt = comment.CreatedAt,
				UpdatedAt = comment.UpdatedAt,
				UserId = comment.UserId,
				UserName = await _context.Users.Where(u => u.Id == userId).Select(u => u.Name).FirstOrDefaultAsync()
			};
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error in AddOrderCommentAsync");
			throw;
		}
	}

	public async Task<OrderCommentDto> AddProductionOrderCommentAsync(Guid orderId, Guid productionOrderId, Guid? userId, string content)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var comment = new Comment
			{
				Id = Guid.NewGuid(),
				OrderId = orderId,
				UserId = userId,
				Content = content,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_context.Comments.Add(comment);
			await _context.SaveChangesAsync();

			var productionOrder = await _context.ProductionOrders.FirstOrDefaultAsync(po => po.Id == productionOrderId);
			if (productionOrder != null)
			{
				productionOrder.CommentIds = (productionOrder.CommentIds ?? new List<Guid>()).Append(comment.Id).ToList();
				productionOrder.UpdatedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}

			await transaction.CommitAsync();

			return new OrderCommentDto
			{
				Id = comment.Id,
				Content = comment.Content,
				CreatedAt = comment.CreatedAt,
				UpdatedAt = comment.UpdatedAt,
				UserId = comment.UserId,
				UserName = await _context.Users.Where(u => u.Id == userId).Select(u => u.Name).FirstOrDefaultAsync()
			};
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error in AddProductionOrderCommentAsync");
			throw;
		}
	}

	public async Task<OrderCommentDto> AddSupplyCommentAsync(Guid orderId, Guid supplyTypeId, Guid? userId, string content)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();

		try
		{
			var orderSupply = await _context.OrderSupplies
				.FirstOrDefaultAsync(os => os.OrderId == orderId);

			if (orderSupply == null)
				throw new Exception("Order supply not found");

			var supplyItem = await _context.SupplyItems
				.FirstOrDefaultAsync(si => si.OrderSupplyId == orderSupply.Id && si.SupplyTypeId == supplyTypeId);

			if (supplyItem == null)
				throw new Exception("Supply item not found");

			var comment = new Comment
			{
				Id = Guid.NewGuid(),
				OrderId = orderId,
				UserId = userId,
				Content = content,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_context.Comments.Add(comment);
			await _context.SaveChangesAsync();

			supplyItem.CommentId = comment.Id;
			supplyItem.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();

			await transaction.CommitAsync();

			return new OrderCommentDto
			{
				Id = comment.Id,
				Content = comment.Content,
				CreatedAt = comment.CreatedAt,
				UpdatedAt = comment.UpdatedAt,
				UserId = comment.UserId,
				UserName = await _context.Users.Where(u => u.Id == userId).Select(u => u.Name).FirstOrDefaultAsync()
			};
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			_logger.LogError(ex, "Error in AddSupplyCommentAsync");
			throw;
		}
	}

	public async Task<OrderCommentDto> UpdateOrderCommentAsync(Guid orderId, Guid commentId, string content)
	{
		var comment = await _context.Comments
			.FirstOrDefaultAsync(c => c.Id == commentId && c.OrderId == orderId);

		if (comment == null)
			throw new Exception("Comment not found or does not belong to this order");

		comment.Content = content;
		comment.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		return new OrderCommentDto
		{
			Id = comment.Id,
			Content = comment.Content,
			CreatedAt = comment.CreatedAt,
			UpdatedAt = comment.UpdatedAt,
			UserId = comment.UserId,
			UserName = await _context.Users.Where(u => u.Id == comment.UserId).Select(u => u.Name).FirstOrDefaultAsync()
		};
	}
}