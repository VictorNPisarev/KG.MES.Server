using System.Reflection.Metadata.Ecma335;
using System.Xml.Serialization;

namespace KG.MES.Main.Models.Xml
{
	public partial class XmlItemMeasurement
	{
		[XmlElement("measure")]
		public List<XmlMeasure>? Measures { get; set; }
	}

	public partial class XmlItemMeasurement
	{
		[XmlIgnore]
		public double Quantity => CalcQuantity();

		/// <summary>
		/// Для совместимости. Изначально завел не список, а только один вложенный элемент, 
		/// т.к. для большинства материалов только одно вложенное измерение. 
		/// А для технических артикулов оказалось, что может быть несколько
		/// </summary>
		[XmlIgnore]
		public XmlMeasure? Measure => Measures?.First();

		private double CalcQuantity()
		{
			if (Measures == null || Measures.Count == 0)
				return 0;

			return Measures.Aggregate(0.0, (sum, measure) => 
			{
				double quantity = measure.Pieces;
				
				if (measure.WidthLength > 0)
				{
					quantity = quantity * measure.WidthLength / 1000.0;
				}

				return sum + quantity;
			});
		}	
	}
}