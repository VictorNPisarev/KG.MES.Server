using System.ComponentModel;
using KG.MES.Shared.Attributes;

namespace KG.MES.Shared.Model.Enums;

public enum NotificationEventType
{
	[EventCode("NEW_ORDER")]
	[Description("Создан новый заказ")]
	NewOrder,

	[EventCode("ORDER_READY")]
	[Description("Заказ переведен в статус Готово")]
	OrderReady,

	[EventCode("SUPPLY_COMPLETE")]
	[Description("Все позиции снабжения получены")]
	SupplyComplete
}