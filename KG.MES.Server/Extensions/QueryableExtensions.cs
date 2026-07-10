using System.Linq.Expressions;
using System.Reflection;

namespace KG.MES.Server.Extensions;

public static class QueryableExtensions
{
	/// <summary>
	/// Универсальная сортировка по имени свойства
	/// </summary>
	public static IOrderedQueryable<T> OrderByProperty<T>(
		this IQueryable<T> source,
		string propertyName,
		string? sortOrder = "asc")
	{
		if (string.IsNullOrEmpty(propertyName))
			propertyName = "ready_date";

		var type = typeof(T);
		var property = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

		if (property == null)
		{
			// Если свойства нет — сортирую по Id
			property = type.GetProperty("Id") ?? type.GetProperties().First();
		}

		var parameter = Expression.Parameter(type, "x");
		var propertyAccess = Expression.MakeMemberAccess(parameter, property);
		var lambda = Expression.Lambda(propertyAccess, parameter);

		var methodName = sortOrder?.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";
		var method = typeof(Queryable).GetMethods()
			.First(m => m.Name == methodName && m.GetParameters().Length == 2)
			.MakeGenericMethod(type, property.PropertyType);

		return (IOrderedQueryable<T>)method.Invoke(null, new object[] { source, lambda })!;
	}

	/// <summary>
	/// Универсальная сортировка с поддержкой вложенных свойств (например, "Customer.Name")
	/// </summary>
	public static IOrderedQueryable<T> OrderByPropertyPath<T>(
		this IQueryable<T> source,
		string propertyPath,
		string? sortOrder = "asc")
	{
		if (string.IsNullOrEmpty(propertyPath))
			propertyPath = "ReadyDate";

		var parameter = Expression.Parameter(typeof(T), "x");
		Expression propertyAccess = parameter;

		foreach (var propName in propertyPath.Split('.'))
		{
			var property = propertyAccess.Type.GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
			if (property == null)
				break;
			propertyAccess = Expression.MakeMemberAccess(propertyAccess, property);
		}

		if (propertyAccess == parameter)
		{
			// Если свойство не найдено — сортирую по Id
			propertyAccess = Expression.MakeMemberAccess(parameter, typeof(T).GetProperty("Id")!);
		}

		var lambda = Expression.Lambda(propertyAccess, parameter);
		var methodName = sortOrder?.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";
		var method = typeof(Queryable).GetMethods()
			.First(m => m.Name == methodName && m.GetParameters().Length == 2)
			.MakeGenericMethod(typeof(T), propertyAccess.Type);

		return (IOrderedQueryable<T>)method.Invoke(null, new object[] { source, lambda })!;
	}
}