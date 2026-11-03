namespace KG.MES.Shared.Services;

public class NotificationStateService
{
	public event Action? OnNewOrders;
	
	public void NotifyNewOrders()
	{
		OnNewOrders?.Invoke();
	}
}