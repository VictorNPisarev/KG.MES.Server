using KG.MES.Main.Models;
using KG.MES.Main.Models.Xml;

namespace KG.MES.Main.Interfaces
{
    public interface IXmlReaderService
    {
        Task<XmlDocumentData> ReadXmlFromContent(string xmlContent);
        Task<XmlDocumentData> ReadXmlFromFile(string filePath);
    }
}