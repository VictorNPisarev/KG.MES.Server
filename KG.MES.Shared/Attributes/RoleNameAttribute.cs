namespace KG.MES.Shared.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class RoleNameAttribute : Attribute
{
	public string Name { get; }
	public RoleNameAttribute(string name) => Name = name;
}