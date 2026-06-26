using System.ComponentModel.DataAnnotations;

namespace KG.MES.Server.Models.Dto;

public class CalculateReadyDateRequestDto
{
	[Required]
	public required string StartDate { get; set; }

	[Range(1, 365, ErrorMessage = "Количество рабочих дней должно быть от 1 до 365")]
	public int WorkingDays { get; set; }
}
