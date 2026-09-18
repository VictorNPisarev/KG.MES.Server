using System.ComponentModel;
using KG.MES.Shared.Common.Attributes;

namespace KG.MES.Main.Models.Enums
{
	public enum FormType
	{
		[Description("Прямоугольная")]
		Rectangular,

		[Description("Арочная")]
		Arch,

		[Description("Косоугольная")]
		Sloped,

		[Description("Не определена")]
		Undefined
	}
}