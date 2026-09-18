using System.ComponentModel;

namespace KG.MES.Main.Models.Enums
{
    /// <summary>
    /// Типы XML-блоков, которые содержат материалы
    /// </summary>
    public enum XmlBlockType
    {
        [Description("Пиломатериалы")]
        XmlWoodProfile,
        
        [Description("Лакокрасочные материалы")]
        XmlPaint,
        
        [Description("Фурнитура")]
        XmlFitting,
        
        [Description("Стеклопакеты")]
        XmlGlassProduct,
        
        [Description("Аксессуары")]
        XmlAccessory,
        
        [Description("Технические артикулы")]
        XmlTechArticle
    }
}