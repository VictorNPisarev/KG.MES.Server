namespace KG.MES.Shared.Common.Attributes;

/// <summary>
/// Помечает DTO totals, указывая его дискриминатор для JSON-десериализации
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TotalsTypeAttribute : Attribute
{
	public string Name { get; }

	public TotalsTypeAttribute(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Totals type name cannot be empty", nameof(name));

		Name = name;
	}
}