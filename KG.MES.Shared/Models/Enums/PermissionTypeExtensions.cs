using System.Reflection;
using KG.MES.Shared.Common.Attributes;
using KG.MES.Shared.Models.Enums;

namespace KG.MES.Shared.Models.Enums;

public static class PermissionTypeExtensions
{
	public static string GetName(this PermissionType permission)
	{
		var field = typeof(PermissionType).GetField(permission.ToString());
		return field?.GetCustomAttribute<PermissionNameAttribute>()?.Name ?? permission.ToString();
	}
}