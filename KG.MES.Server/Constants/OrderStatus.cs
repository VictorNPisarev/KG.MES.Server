namespace KG.MES.Server.Constants;

public static class OrderStatus
{
	public static class WorkplaceStatus //TODO после объединения переноса всех проектов в этот solution сделать так, чтобы в запрос api статус брался через константы этого класса
	{
		public const string Planned = "planned";
		public const string Joinery = "joinery";
		public const string Pending = "pending";
		public const string Active = "active";
		public const string Completed = "completed";

		public static readonly List<string> All = new() { Planned, Joinery, Pending, Active, Completed };

		public static readonly Dictionary<string, int> Level = new()
		{
			{ Planned, 10 },
			{ Joinery, 20 },
			{ Pending, 30 },
			{ Active, 40 },
			{ Completed, 50 }
		};

		public static bool CanTransition(string from, string to)
		{
			if (!Level.ContainsKey(from.ToLower()) || !Level.ContainsKey(to.ToLower()))
				return false;
			return Level[to] > Level[from];
		}
	}

	public static class CommonStatus
	{
		public const string Complete = "ГОТОВО";
		public const string Departure = "Отгружен";
		public const string InWork = "В работе";
		public const string None = "none";

	}
}