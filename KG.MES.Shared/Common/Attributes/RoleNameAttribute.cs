namespace KG.MES.Shared.Common.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class RoleNameAttribute : Attribute
{
	public string Name { get; }
	public RoleNameAttribute(string name) => Name = name;
}