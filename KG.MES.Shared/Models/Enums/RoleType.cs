using KG.MES.Shared.Common.Attributes;

namespace KG.MES.Shared.Models.Enums;

public enum RoleType
{
	[RoleName("Admin")]
	Admin,

	[RoleName("Advanced")]
	Advanced,

	[RoleName("Middle")]
	Middle,

	[RoleName("Simple")]
	Simple,

	[RoleName("LumberSupply")]
	LumberSupply
}