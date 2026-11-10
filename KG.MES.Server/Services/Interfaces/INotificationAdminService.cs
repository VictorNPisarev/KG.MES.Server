using KG.MES.Shared.Models.Dto;

namespace KG.MES.Server.Services.Interfaces;

public interface INotificationAdminService
{
	Task<List<NotificationSubscriptionDto>> GetSubscriptionsAsync();
	Task<Guid> CreateSubscriptionAsync(NotificationSubscribeRequestDto dto);
	Task<bool> ToggleSubscriptionAsync(Guid id);
	Task<bool> DeleteSubscriptionAsync(Guid id);
	Task<bool> UpdateSubscriptionSeverityAsync(Guid id, string severity);
	Task<List<NotificationEventDto>> GetEventsAsync();
	Task<List<RoleDto>> GetRolesAsync();
}