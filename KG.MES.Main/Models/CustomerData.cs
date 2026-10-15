using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using KG.MES.Main.Models.Xml;

namespace KG.MES.Main.Models
{
	public class CustomerData
	{
		///<summary>
		///Уникальный номер контрагента
		///</sunnary> 
		[Required(ErrorMessage = "Контрагент обязателен.")]
		public string? CustomerNumber { get; set; }
		
		///<summary>
		///Имя контрагента
		///</sunnary> 
		public string? FirstName { get; set; }
		
		///<summary>
		///Фамилия контрагента
		///</sunnary> 
		public string? LastName { get; set; }
		
		///<summary>
		///Адрес - улица
		///</sunnary> 
		public string? Street { get; set; }
		
		///<summary>
		///Адрес - индекс
		///</sunnary> 
		public string? Postcode { get; set; }
		
		///<summary>
		///Адрес - город
		///</sunnary> 
		public string? City { get; set; }
		
		///<summary>
		///Адрес - ISO код страны
		///</sunnary> 
		public string? CountryISO { get; set; }
		
		// Контакты - напрямую как коллекция
		public List<XmlContactInfo> Contacts { get; set; } = new List<XmlContactInfo>();
		
		// Удобные свойства
		public string? Email => Contacts?.FirstOrDefault(c => c.ContactType == "email")?.Value;
		
		public string? Phone => Contacts?.FirstOrDefault(c => c.ContactType == "mobile")?.Value;

		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
				throw new ArgumentException("Document address must have at least first name or last name");
		}

		public CustomerData() {}

		public CustomerData(XmlDocumentAddress xmlData)
		{
			this.CustomerNumber = xmlData.BusinessPartnerNumber;
			this.FirstName = xmlData.FirstName;
			this.LastName = xmlData.LastName;
			this.Street = xmlData.Street;
			this.Postcode = xmlData.Postcode;
			this.City = xmlData.City;
			this.CountryISO = xmlData.CountryISO;
			this.Contacts = xmlData.Contacts;
		}
	}
}