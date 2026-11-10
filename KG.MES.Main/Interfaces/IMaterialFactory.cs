using KG.MES.Main.Models;

namespace KG.MES.Main.Interfaces
{
	public interface IMaterialFactory
	{
		Material? CreateMaterial(IXmlDataMaterial xmlMaterial, DocumentItem parent);
	}
}