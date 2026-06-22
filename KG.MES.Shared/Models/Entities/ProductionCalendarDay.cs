using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KG.MES.Shared.Models.Entities;

[Table("production_calendar")]
public class ProductionCalendarDay
{
	[Key]
	[Column("calendar_date")]
	public DateTime CalendarDate { get; set; }

	[Column("is_working_day")]
	public bool IsWorkingDay { get; set; }

	[Column("is_shortened_day")]
	public bool IsShortenedDay { get; set; }

	[Column("description")]
	public string? Description { get; set; }
}