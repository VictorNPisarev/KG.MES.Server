using KG.MES.Shared.Models.Enums;

public class AdminPageConfig
{
	public bool AllowCreateLicense { get; set; }
	public bool AllowRevokeLicense { get; set; }
	public bool AllowExtendLicense { get; set; }
	public bool AllowCreateUser { get; set; }
	public bool AllowBlockUser { get; set; }
	public bool AllowSetRole { get; set; }
	public bool AllowResetPassword { get; set; }
	public string[] AllowedRolesToCreate { get; set; } = ["User"];
	public LicenseType[] AllowedLicenseTypes { get; set; } = [LicenseType.SingleDevice, LicenseType.MultiDevice];
}