using KG.MES.Shared.Models.Dto;

namespace KG.MES.Shared.Common.Extensions;

public static class OrderNotificationDtoExtensions
{
	public static int GetSeverityOrder(this OrderNotificationDto orderNotificationDto) => orderNotificationDto.Severity switch
	{
		"Danger" => 0,
		"Warning" => 1,
		"Success" => 2,
		_ => 3
	};
}
