using System.ComponentModel;

namespace KG.MES.Main.Models
{
	public enum AccessoryType
	{
		[Description("В составе конструкции")]
		ArtTech,
		[Description("Дополнение к позиции")]
		AccessoryItem
	}
}