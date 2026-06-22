namespace KG.MES.Shared.Models.Dto;

public class CalculateReadyDateResponseDto
{
	public DateTime StartDate { get; set; }
	public DateTime EndDate { get; set; }
	public int WorkingDays { get; set; }
	public int CalendarDays { get; set; }
}