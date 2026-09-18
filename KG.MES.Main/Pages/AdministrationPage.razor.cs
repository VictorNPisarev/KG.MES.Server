using KG.MES.Shared.Models.Enums;
using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace KG.MES.Main.Pages;

public partial class AdministrationPage
{
	[Inject] private UserSessionService Session { get; set; } = null!;

	private AdminPageConfig Config => new()
	{
		AllowCreateLicense = true,
		AllowRevokeLicense = true,
		AllowExtendLicense = true,
		AllowCreateUser = true,
		AllowBlockUser = true,
		AllowSetRole = true,
		AllowResetPassword = true,
		AllowedRolesToCreate = ["Worker", "Master"],
		AllowedLicenseTypes = [LicenseType.SingleDevice, LicenseType.MultiDevice]
	};
}
