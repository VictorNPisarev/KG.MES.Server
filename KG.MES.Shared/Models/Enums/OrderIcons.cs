namespace KG.MES.Shared.Models.Enums;

public enum OrderIcon
{
	[IconClass("bi bi-exclamation-triangle-fill", "order-icon-claim", "Рекламация")]
	Claim,

	[IconClass("bi bi-piggy-bank-fill", "order-icon-econom", "Эконом")]
	Econom,

	[IconClass("bi bi-cash-stack", "order-icon-paid", "Оплачен, не запущен")]
	Paid,

	[IconClass("bi-arrows-collapse-vertical", "icon-double-paint-classic", "Двухсторонняя покраска")]
	TwoSidePaint,

	[IconClass("bi-stack", "order-icon-plate", "Только щитовые")]
	Plate,

	[IconClass("bi bi-box-arrow-in-right", "icon-transfer-start", "Переходящий заказ. Взят в работу раньше выбранного периода")]
	StartOperationTransfer,

	[IconClass("bi bi-box-arrow-right", "icon-transfer-complete", "Переходящий заказ. Завершен позже выбранного периода")]
	CompleteOperationTransfer
}