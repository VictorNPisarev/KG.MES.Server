using KG.MES.Main.Interfaces;
using KG.MES.Main.Models;

namespace KG.MES.Main.Services
{
	public class MaterialFactory : IMaterialFactory
	{
		private readonly MaterialTypeConfigService _materialTypeConfigService;

		public MaterialFactory(MaterialTypeConfigService materialTypeConfigService)
		{
			_materialTypeConfigService = materialTypeConfigService;
		}

		public Material? CreateMaterial(IXmlDataMaterial xmlMaterial, DocumentItem parent)
		{
			return Material.FromXmlDataMaterial(xmlMaterial, parent, _materialTypeConfigService);
		}
	}
}