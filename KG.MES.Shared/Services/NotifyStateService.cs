namespace KG.MES.Shared.Services;

public class NotificationStateService
{
	// Событие для обновления списка новых заказов
	public event Action? OnNewOrders;

	// Если появятся другие события — добавляешь здесь
	// public event Action<Guid>? OnOrderCommentAdded;

	public void NotifyNewOrders() => OnNewOrders?.Invoke();
	// public void NotifyOrderCommentAdded(Guid orderId) => OnOrderCommentAdded?.Invoke(orderId);
}