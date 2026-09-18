using System.Xml.Serialization;
using KG.MES.Main.Interfaces;


namespace KG.MES.Main.Models.Xml
{
	[XmlType("delivery_address")]
	public class XmlDeliveryAddress : IXmlDataModel
	{
		[XmlElement("business_partner_number")]
		public string? BusinessPartnerNumber { get; set; }
		
		[XmlElement("first_name")]
		public string? FirstName { get; set; }
		
		[XmlElement("name")]
		public string? LastName { get; set; }
		
		[XmlElement("street")]
		public string? Street { get; set; }
		
		[XmlElement("postcode")]
		public string? Postcode { get; set; }
		
		[XmlElement("city")]
		public string? City { get; set; }
		
		[XmlElement("country")]
		public string? Country { get; set; }
		
		public void Validate()
		{
		}
	}
}