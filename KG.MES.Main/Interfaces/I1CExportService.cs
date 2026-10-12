using KG.MES.Main.Models;
using KG.MES.Main.Models.Xml;

namespace KG.MES.Main.Interfaces
{
    public interface I1CExportService
    {
        Task<bool> ExportOrderAsync(XmlDocumentData order);
        Task<string> TestConnectionAsync();
    }
}