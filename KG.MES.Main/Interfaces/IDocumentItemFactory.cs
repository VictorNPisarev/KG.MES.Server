using KG.MES.Main.Models;
using KG.MES.Main.Models.Xml;

namespace KG.MES.Main.Interfaces
{
	public interface IDocumentItemFactory
	{
		DocumentItem Create(XmlDocumentItem xmlData, DocumentData? documentData = null);
	}
}