using System.Xml.Serialization;
using KG.MES.Main.Interfaces;

namespace KG.MES.Main.Models.Xml
{
    [XmlType("document_address")]
    public class XmlDocumentAddress : IXmlDataModel
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
        
        [XmlElement("country_isocode")]
        public string? CountryISO { get; set; }
        
        // Контакты - напрямую как коллекция
        [XmlArray("it_contact_list")]
        [XmlArrayItem("it_contact")]
        public List<XmlContactInfo> Contacts { get; set; } = new List<XmlContactInfo>();
        
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
                throw new ArgumentException("Document address must have at least first name or last name");
        }
    }
}