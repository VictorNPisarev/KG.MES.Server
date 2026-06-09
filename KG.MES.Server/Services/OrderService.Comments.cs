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
		// Ищем комментарии напрямую через OrderId (надежнее, чем через массив CommentIds)
		// Используем LEFT JOIN для Users, чтобы не терять комментарии без UserId
		var comments = await _context.Comments
			.Where(c => c.OrderId == orderId)
			.GroupJoin(_context.Users,
				c => c.UserId,
				u => u.Id,
				(c, users) => new { Comment = c, Users = users })
			.SelectMany(
				x => x.Users.DefaultIfEmpty(),
				(x, u) => new OrderCommentDto
				{
					Id = x.Comment.Id,
					Content = x.Comment.Content,
					CreatedAt = x.Comment.CreatedAt,
					UpdatedAt = x.Comment.UpdatedAt,
					UserName = u != null ? u.Name : null
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
				order.CommentIds = [.. order.CommentIds ?? [], comment.Id];
				order.UpdatedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}

			await transaction.CommitAsync();

			//try
			//{
			//	await NotificationHelper.OrderCommentAdded(orderId, comment.Id, content, userId);
			//}
			//catch (Exception ex)
			//{
			//	_logger.LogWarning(ex, "Failed to send notification for comment");
			//}
			
			return new OrderCommentDto
			{
				Id = comment.Id,
				Content = comment.Content,
				CreatedAt = comment.CreatedAt,
				UpdatedAt = comment.UpdatedAt,
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
				productionOrder.CommentIds = [.. productionOrder.CommentIds ?? [], comment.Id];
				productionOrder.UpdatedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}

			await transaction.CommitAsync();

			//try
			//{
			//	await NotificationHelper.OrderCommentAdded(orderId, comment.Id, content, userId);
			//}
			//catch (Exception ex)
			//{
			//	_logger.LogWarning(ex, "Failed to send notification for comment");
			//}


			return new OrderCommentDto
			{
				Id = comment.Id,
				Content = comment.Content,
				CreatedAt = comment.CreatedAt,
				UpdatedAt = comment.UpdatedAt,
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

			//try
			//{
			//	await NotificationHelper.OrderCommentAdded(orderId, comment.Id, content, userId);
			//}
			//catch (Exception ex)
			//{
			//	_logger.LogWarning(ex, "Failed to send notification for comment");
			//}


			return new OrderCommentDto
			{
				Id = comment.Id,
				Content = comment.Content,
				CreatedAt = comment.CreatedAt,
				UpdatedAt = comment.UpdatedAt,
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

		//try
		//{
		//	await NotificationHelper.OrderCommentAdded(orderId, commentId, content, null);
		//}
		//catch (Exception ex)
		//{
		//	_logger.LogWarning(ex, "Failed to send notification for comment");
		//}


		return new OrderCommentDto
		{
			Id = comment.Id,
			Content = comment.Content,
			CreatedAt = comment.CreatedAt,
			UpdatedAt = comment.UpdatedAt,
			UserName = await _context.Users.Where(u => u.Id == comment.UserId).Select(u => u.Name).FirstOrDefaultAsync()
		};
	}
}