using System.Linq;
using System.Xml.Serialization;
using KG.MES.Main.Interfaces;

namespace KG.MES.Main.Models.Xml
{
	public partial class XmlWoodProfile : IXmlDataModel
	{
		[XmlElement("component")]
		public string? Component { get; set; }
		
		[XmlElement("frame_nr")]
		public int FrameNumber { get; set; }
		
		[XmlElement("field_nr")]
		public int FieldNumber { get; set; }
		
		[XmlElement("windows_form")]
		public string? WindowsForm { get; set; }
		
		[XmlElement("profile_nr")]
		public string? ProfileNumber { get; set; }
		
		[XmlElement("hersteller")]
		public string? Manufacturer { get; set; }
		
		[XmlElement("profilegroup")]
		public string? ProfileGroup { get; set; }
		
		[XmlElement("profile_desc")]
		public string? Description { get; set; }
		
		[XmlElement("lieferant")]
		public string? Supplier { get; set; }
		
		[XmlElement("deliverykind")]
		public string? DeliveryKind { get; set; }
		
		[XmlElement("connection1")]
		public string? Connection1 { get; set; }
		
		[XmlElement("connection2")]
		public string? Connection2 { get; set; }
		
		[XmlElement("profile_use")]
		public string? ProfileUse { get; set; }
		
		[XmlElement("profile_type")]
		public string? ProfileType { get; set; }
		
		[XmlArray("profiledetails")]
		[XmlArrayItem("profiledetaildata")]
		public List<XmlProfileDetailData> xmlProfileDetailDatas { get; set; } = new();

		public void Validate() 
		{
			if (string.IsNullOrWhiteSpace(ProfileNumber))
				throw new ArgumentException("Profile number is required");
				
			if (string.IsNullOrWhiteSpace(ProfileGroup))
				throw new ArgumentException("Profile group is required");
		}
	}

	/// <summary>
	/// Поля из вложенных блоков
	/// </summary>
	public partial class XmlWoodProfile
	{
		public string TimberKind
		{
			get 
			{
				string lumberKinds = string.Join('/', xmlProfileDetailDatas.Select(p => p.TimberKindDescription).ToList()); //string.Empty;

				return lumberKinds; 
			}
		}
	}
}