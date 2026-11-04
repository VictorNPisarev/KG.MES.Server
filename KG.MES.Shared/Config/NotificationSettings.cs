public class NotificationSettings
{
	public Dictionary<string, bool> ExpandByDefault { get; set; } = new()
	{
		["Danger"] = true,
		["Warning"] = false,
		["Success"] = false,
		["Info"] = false
	};
}