using KG.MES.Shared.Models.Enums;
using KG.MES.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace KG.MES.Masters.Components.Pages;

public partial class AdministrationPage
{
	[Inject] private UserSessionService Session { get; set; } = null!;

	private AdminPageConfig Config => new()
	{
		AllowCreateLicense = false,
		AllowRevokeLicense = false,
		AllowExtendLicense = false,
		AllowCreateUser = Session.User?.RoleName == "Admin" || Session.User?.RoleName == "Master",
		AllowBlockUser = false,
		AllowSetRole = false,
		AllowResetPassword = false,
		AllowedRolesToCreate = ["Worker", "Master"],
		AllowedLicenseTypes = [LicenseType.SingleDevice]
	};
}