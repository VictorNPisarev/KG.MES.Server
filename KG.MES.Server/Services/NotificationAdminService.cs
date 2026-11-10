using KG.MES.Server.Data;
using KG.MES.Server.Services.Interfaces;
using KG.MES.Shared.Models.Dto;
using KG.MES.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KG.MES.Server.Services;

public class NotificationAdminService : INotificationAdminService
{
	private readonly AppDbContext _context;

	public NotificationAdminService(AppDbContext context)
	{
		_context = context;
	}

	public async Task<List<NotificationSubscriptionDto>> GetSubscriptionsAsync()
	{
		return await _context.NotificationSubscriptions
			.Include(s => s.Event)
			.Include(s => s.Role)
			.Include(s => s.User)
			.OrderBy(s => s.CreatedAt)
			.Select(s => new NotificationSubscriptionDto
			{
				Id = s.Id,
				EventId = s.EventId,
				EventCode = s.Event!.EventCode,
				EventDescription = s.Event.Description,
				RoleId = s.RoleId,
				RoleName = s.Role != null ? s.Role.Name : null,
				UserId = s.UserId,
				UserName = s.User != null ? s.User.Name : null,
				Severity = s.Severity,
				IsActive = s.IsActive
			})
			.ToListAsync();
	}

	public async Task<Guid> CreateSubscriptionAsync(NotificationSubscribeRequestDto dto)
	{
		var subscription = new NotificationSubscription
		{
			Id = Guid.NewGuid(),
			EventId = dto.EventId,
			RoleId = dto.RoleId,
			UserId = dto.UserId,
			Severity = dto.Severity,
			IsActive = true
		};

		_context.NotificationSubscriptions.Add(subscription);
		await _context.SaveChangesAsync();

		return subscription.Id;
	}

	public async Task<bool> ToggleSubscriptionAsync(Guid id)
	{
		var subscription = await _context.NotificationSubscriptions.FindAsync(id);
		if (subscription == null) return false;

		subscription.IsActive = !subscription.IsActive;
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> DeleteSubscriptionAsync(Guid id)
	{
		var subscription = await _context.NotificationSubscriptions.FindAsync(id);
		if (subscription == null) return false;

		_context.NotificationSubscriptions.Remove(subscription);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<List<NotificationEventDto>> GetEventsAsync()
	{
		return await _context.NotificationEvents
			.Select(e => new NotificationEventDto { Id = e.Id, EventCode = e.EventCode, Description = e.Description })
			.ToListAsync();
	}

	public async Task<List<RoleDto>> GetRolesAsync()
	{
		return await _context.Roles
			.Select(r => new RoleDto { Id = r.Id, Name = r.Name })
			.ToListAsync();
	}

	public async Task<bool> UpdateSubscriptionSeverityAsync(Guid id, string severity)
	{
		var subscription = await _context.NotificationSubscriptions.FindAsync(id);
		if (subscription == null) return false;

		if (string.IsNullOrWhiteSpace(severity))
			return false;

		subscription.Severity = severity;
		await _context.SaveChangesAsync();
		return true;
	}
}