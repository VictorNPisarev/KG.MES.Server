namespace KG.MES.Shared.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class PermissionNameAttribute : Attribute
{
	public string Name { get; }
	public PermissionNameAttribute(string name) => Name = name;
}