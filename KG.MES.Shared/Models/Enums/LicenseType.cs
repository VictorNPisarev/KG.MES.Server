namespace KG.MES.Shared.Models.Enums;
public enum LicenseType
{
	SingleDevice = 0,  // 1 устройство на лицензию
	MultiDevice = 1    // MaxDevices штук (или безлимит, если null)
}