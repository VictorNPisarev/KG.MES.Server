namespace KG.MES.UI.Shared.Interfaces
{
	public interface ISavableWidget
	{
		bool HasUnsavedChanges();
		Task SaveAllAsync();
	}
}