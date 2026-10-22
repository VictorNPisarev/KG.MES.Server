using System.Xml.Serialization;
using KG.MES.Main.Interfaces;

namespace KG.MES.Main.Models.Xml
{
	[XmlRoot("document_data")]
	public class XmlDocumentData : IXmlDataModel
	{
		[XmlElement("document_kind")]
		public string? DocumentKind { get; set; }
		
		[XmlElement("document_number")]
		public string? DocumentNumber { get; set; }
		
		[XmlElement("description")]
		public string? Description { get; set; }
		
		[XmlElement("customer_number")]
		public string? CustomerNumber { get; set; }
		
		[XmlElement("authorised_person_name")]
		public string? AuthorisedPersonName { get; set; }
		
		[XmlElement("document_date")]
		public string? DocumentDate { get; set; }
		
		[XmlElement("project_number")]
		public string? ProjectNumber { get; set; }
		
		[XmlElement("project_description")]
		public string? ProjectDescription { get; set; }
		
		[XmlElement("document_amount_net")]
		public decimal DocumentAmountNet { get; set; }
		
		[XmlElement("document_amount_gross")]
		public decimal DocumentAmountGross { get; set; }
		
		[XmlElement("document_address")]
		public XmlDocumentAddress? XmlDocumentAddress { get; set; }
		
		[XmlElement("delivery_address")]
		public XmlDeliveryAddress? xmlDeliveryAddress { get; set; }
		
		[XmlArray("document_items")]
		[XmlArrayItem("item")]
		public List<XmlDocumentItem> xmlDocumentItems { get; set; } = new();

		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(DocumentNumber))
				throw new ArgumentException("Document number is required");
			
			XmlDocumentAddress?.Validate();
			xmlDeliveryAddress?.Validate();
			
			foreach (var item in xmlDocumentItems)
				item.Validate();
		}
	}
}