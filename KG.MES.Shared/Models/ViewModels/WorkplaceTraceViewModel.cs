
namespace KG.MES.Shared.Models.ViewModels;

public class WorkplaceTraceViewModel
{
	public Guid WorkplaceId { get; set; }

	public string WorkplaceName { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;
}

public static class WorkplaceTraceExtensions
{
	public static string GetStatusText(this WorkplaceTraceViewModel trace) => trace.Status.ToLower() switch
	{
		"planned" => "Не определен",
		"pending" => "Ожидает",
		"active" => "В работе",
		"completed" => "Завершён",
		"joinery" => "Ожидает",
		_ => trace.Status ?? "—"
	};
}
