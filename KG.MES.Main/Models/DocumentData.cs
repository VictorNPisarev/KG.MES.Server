using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using KG.MES.Main.Models.Xml;
using KG.MES.Main.Common.Extensions;
using KG.MES.Main.Models.Enums;
using KG.MES.Main.Services;
using KG.MES.Main.Interfaces;

namespace KG.MES.Main.Models
{
	public partial class DocumentData
	{
		/// <summary>
		/// Идентификатор
		/// </summary>
		public Guid Id { get; set; } = new();

		private List<string>? uniqueColors;
		private List<string>? uniqueColorsAll;

		///<summary>
		///Тип заказа
		///</summary>
		public string? DocumentKind { get; set; }
		
		///<summary>
		///Номер заказа
		///</summary>
		[Required(ErrorMessage = "№ заказа обязателен.")]
		public string? DocumentNumber { get; set; }
		
		///<summary>
		///Описание заказа
		///</summary>
		public string? Description { get; set; }
		
		///<summary>
		///Сотрудник, создавший заказ
		///</summary>
		public string? AuthorisedPersonName { get; set; }
		
		///<summary>
		///Дата заказа
		///</summary>
		public DateTime? DocumentDate { get; set; }
		
		///<summary>
		///Номер проекта
		///</summary>
		public string? ProjectNumber { get; set; }
		
		///<summary>
		///Описание проекта
		///</summary>
		public string? ProjectDescription { get; set; }
		
		///<summary>
		///Итоговая сумма
		///</summary>
		public decimal DocumentAmountGross { get; set; }

		///<summary>
		///Заказ-эконом
		///</summary>
		public bool IsEconom { get; set; } = false;

		///<summary>
		///Заказ-рекламация
		///</summary>
		public bool IsClaim { get; set; } = false;

		///<summary>
		///Заказ оплачен, но еще не запущен в производство
		///</summary>
		public bool IsOnlyPaid { get; set; } = false;

		///<summary>
		///Заказ оплачен, но еще не запущен в производство
		///</summary>
		public string? Comment { get; set; }


		///<summary>
		///Контрагент
		///</summary>
		public CustomerData? CustomerData { get; set; }
		
		///<summary>
		///Позиции заказа
		///</summary>
		public List<DocumentItem> DocumentItems { get; set; } = new();
		
		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(DocumentNumber))
				throw new ValidateException("Document number is required");
			
			CustomerData?.Validate();
			//DeliveryAddress?.Validate();
			
			foreach (var item in DocumentItems)
				item.Validate();
		}
		
		public DocumentData() {}
		public DocumentData(XmlDocumentData xmlData, IDocumentItemFactory itemFactory)
		{
			this.DocumentKind = xmlData.DocumentKind;
			this.DocumentNumber = xmlData.DocumentNumber;
			this.Description = xmlData.Description;
			this.AuthorisedPersonName = xmlData.AuthorisedPersonName;
			this.DocumentDate = xmlData.DocumentDate?.ParseXmlDate();
			this.ProjectNumber = xmlData.ProjectNumber;
			this.ProjectDescription = xmlData.ProjectDescription;
			this.DocumentAmountGross = xmlData.DocumentAmountGross;
			this.CustomerData = xmlData.XmlDocumentAddress != null ? new(xmlData.XmlDocumentAddress) : new();

			foreach (var documentItem in xmlData.xmlDocumentItems)
			{
				this.DocumentItems.Add(itemFactory.Create(documentItem, this));
			}

			this.Validate();
		}
	}

	/// <summary>
	/// Вычисляемые сфойства и методы
	/// </summary>
	public partial class DocumentData
	{
		/// <summary>
		/// Согласованный срок производства (кол-во дней)
		/// </summary>
		public int ApprovedLeadTimeDays { get; set; } = 55;

		/// <summary>
		/// Согласованный срок производства (кол-во дней)
		/// </summary>
		public DateTime InitializeDate { get; set; } = DateTime.Now;

		/// <summary>
		/// Согласованная дата готовности
		/// </summary>
		public DateTime? ReadyDate { get; set; }

		/// <summary>
		/// Список цветов покраски (внеш + внутр)
		/// </summary>
		public List<string> UniqueColors
		{
			get
			{
				uniqueColors ??= DocumentItems
					.Select(i => i.Color ?? string.Empty)
					.Where(c => !string.IsNullOrEmpty(c))
					.Distinct()
					.ToList();
				return uniqueColors;
			}
		}
		
		/// <summary>
		/// Список всех цветов покраски одной строкой
		/// </summary>
		public string UniqueColorsDisplay => string.Join(", ", UniqueColors);

		/// <summary>
		/// Список всех уникальных цветов
		/// </summary>
		public List<string> UniqueColorsAll
		{
			get
			{
				uniqueColorsAll ??= DocumentItems
					.SelectMany(i => i.Color?.Split('/') ?? Array.Empty<string>())
					.Where(c => !string.IsNullOrWhiteSpace(c))
					.Select(c => c.Trim())
					.Distinct()
					.ToList();
				return uniqueColorsAll;
			}
		}
		
		// Метод для сброса кэша (если Items изменился)
		public void InvalidateCache()
		{
			uniqueColors = null;
			uniqueColorsAll = null;
		}

	}

	public static class DocumentDataExtensions
	{
		public static bool IsDoubleColor (this DocumentData documentData)
		{
			bool result = false;
			
			foreach(var documentItem in documentData.DocumentItems)
			{
				result = result || documentItem.DoubleColor();
			}

			return result;
		}

		public static List<DocumentItem> GetItemsByType(this DocumentData data, DocumentItemType type)
		{
			return data.DocumentItems
				.Where(i => i.ItemType == type)
				.ToList();
		}
		
		public static Dictionary<DocumentItemType, List<DocumentItem>> GroupByTypes(this DocumentData data)
		{
			return data.DocumentItems
				.GroupBy(i => i.ItemType)
				.ToDictionary(g => g.Key, g => g.ToList());
		}

		public static DateTime? CalcReadyDate(this DocumentData data, HolidayCalendarService holidayService)
		{
			return data.ReadyDate = holidayService.AddBusinessDays(data.InitializeDate, data.ApprovedLeadTimeDays);
		}
	}
}