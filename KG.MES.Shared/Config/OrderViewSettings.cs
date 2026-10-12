using System.Text.Json.Serialization;

namespace KG.MES.Shared.Models.Config
{
	public class OrderViewSettings
	{
		/// <summary>
		/// Точка доступа api для загрузки списка заказов
		/// </summary>
		[JsonPropertyName("listEndpoint")]
		public string ListEndpoint { get; set; } = "orders";

		/// <summary>
		/// Точка доступа api для загрузки карточки заказа
		/// </summary>
		[JsonPropertyName("cardEndpoint")]
		public string CardEndpoint { get; set; } = "orders";

		/// <summary>
		/// Заголовок формы
		/// </summary>
		[JsonPropertyName("title")]
		public string Title { get; set; } = "Заказы";

		/// <summary>
		/// Показывать панель кнопок с дополнительными действиями (загрузить XML,)
		/// </summary>
		[JsonPropertyName("showActions")]
		public bool ShowActions { get; set; } = true;

		/// <summary>
		/// Права на редактирование
		/// </summary>
		[JsonPropertyName("canEdit")]
		public bool CanEdit { get; set; }

		/// <summary>
		/// Права на удаление
		/// </summary>
		[JsonPropertyName("canDelete")]
		public bool CanDelete { get; set; }

		/// <summary>
		/// Права на экспорт
		/// </summary>
		[JsonPropertyName("canExport")]
		public bool CanExport { get; set; }

		/// <summary>
		/// Права на просмотр этапов производства
		/// </summary>
		[JsonPropertyName("showTrace")]
		public bool ShowTrace { get; set; } = true;

		/// <summary>
		/// Права на изменение этапов производства
		/// </summary>
		[JsonPropertyName("editTrace")]
		public bool EditTrace { get; set; }

		/// <summary>
		/// Права на видимость отметок снабжения
		/// </summary>
		[JsonPropertyName("showSupply")]
		public bool ShowSupply { get; set; } = true;

		/// <summary>
		/// Права на изменение отметок снабжения
		/// </summary>
		[JsonPropertyName("editSupply")]
		public bool EditSupply { get; set; }

		//public bool UseSplitView { get; set; } = false;  // false = модалка, true = split-view

		/// <summary>
		/// ???
		/// </summary>
		[JsonPropertyName("additional")]
		public Dictionary<string, object> Additional { get; set; } = new();
	}
}