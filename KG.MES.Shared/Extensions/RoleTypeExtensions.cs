using System.Reflection;
using KG.MES.Shared.Attributes;
using KG.MES.Shared.Models.Enums;

namespace KG.MES.Shared.Extensions;

public static class RoleTypeExtensions
{
	public static string GetName(this RoleType role)
	{
		var field = typeof(RoleType).GetField(role.ToString());
		return field?.GetCustomAttribute<RoleNameAttribute>()?.Name ?? role.ToString();
	}

	public static RoleType? FromName(string? name)
	{
		if (string.IsNullOrEmpty(name)) return null;

		foreach (RoleType role in Enum.GetValues(typeof(RoleType)))
		{
			if (role.GetName() == name) return role;
		}
		return null;
	}
}