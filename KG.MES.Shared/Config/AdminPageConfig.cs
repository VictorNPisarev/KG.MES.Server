using KG.MES.Shared.Models.Enums;

namespace KG.MES.Shared.Config;

public class AdminPageConfig
{
	public bool AllowCreateLicense { get; set; }
	public bool AllowRevokeLicense { get; set; }
	public bool AllowExtendLicense { get; set; }
	public bool AllowCreateUser { get; set; }
	public bool AllowBlockUser { get; set; }
	public bool AllowSetRole { get; set; }
	public bool AllowResetPassword { get; set; }

	// Оповещения
	public bool AllowViewNotifications { get; set; } = true;
	public bool AllowCreateNotification { get; set; } = true;
	public bool AllowToggleNotification { get; set; } = true;
	public bool AllowDeleteNotification { get; set; } = true;
	public bool AllowEditNotificationSeverity { get; set; } = true;


	public string[] AllowedRolesToCreate { get; set; } = ["User"];
	public LicenseType[] AllowedLicenseTypes { get; set; } = [LicenseType.SingleDevice, LicenseType.MultiDevice];
}