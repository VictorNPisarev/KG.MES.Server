namespace KG.MES.Server.Constants;

public static class OrderStatus
{
	public static class WorkplaceStatus
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
			if (!Level.ContainsKey(from) || !Level.ContainsKey(to))
				return false;
			return Level[to] > Level[from];
		}
	}
}