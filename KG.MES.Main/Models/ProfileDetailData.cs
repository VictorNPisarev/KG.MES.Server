using System.Xml.Serialization;
using KG.MES.Main.Models.Xml;

namespace KG.MES.Main.Models
{
	public class ProfileDetailData
	{
		/// <summary>
		/// Вид дерева
		/// </summary>
		public string? TimberKind { get; set; }
		
		/// <summary>
		/// Описание дерева
		/// </summary>
		public string? TimberKindDescription { get; set; }
		
		/// <summary>
		/// Конечная ширина
		/// </summary>
		public decimal FinishedWidth { get; set; }
		
		/// <summary>
		/// Штрина разрезки
		/// </summary>
		public decimal CuttingWidth { get; set; }
		
		/// <summary>
		/// Ширина шлифовки
		/// </summary>
		public decimal PlaneWidth { get; set; }
		
		/// <summary>
		/// Конечная толщина
		/// </summary>
		public decimal FinishedThickness { get; set; }
		
		/// <summary>
		/// Толщина разрезки
		/// </summary>
		public decimal CuttingThickness { get; set; }
		
		/// <summary>
		/// Толщина шлифовки
		/// </summary>
		public decimal PlaneThickness { get; set; }
		
		/// <summary>
		/// Процент отхода
		/// </summary>
		public decimal CuttingsPercentage { get; set; }
		
		/// <summary>
		/// Ед. измерения
		/// </summary>
		public string? ProfileUnit { get; set; }
		
		/// <summary>
		/// Отрезок бруса
		/// </summary>
		public decimal CuttingLength { get; set; }
		
		/// <summary>
		/// Обозначение бруса
		/// </summary>
		public string? SquareNr { get; set; }
		
		/// <summary>
		/// Брус - вид дерева
		/// </summary>
		public string? SquareTimberKind { get; set; }

		/// <summary>
		/// Ширина бруса
		/// </summary>
		public decimal SquareWidth { get; set; }

		/// <summary>
		/// Толщина бруса
		/// </summary>
		public decimal SquareThickness { get; set; }

		/// <summary>
		/// Длина бруса
		/// </summary>
		public decimal SquareLength { get; set; }

		/// <summary>
		/// Цена бруса
		/// </summary>
		public decimal SquarePrice { get; set; }

		/// <summary>
		/// Количество брусков
		/// </summary>
		public int Piece { get; set; }
		
		/// <summary>
		/// Конечная длина
		/// </summary>
		public decimal FinishedLength { get; set; }

		/// <summary>
		/// Профиль
		/// </summary>
		public WoodProfile? WoodProfile { get; set; }

		public ProfileDetailData () {}

		public ProfileDetailData (XmlProfileDetailData xmlProfileDetail, WoodProfile? woodProfile = null)
		{
			TimberKind = xmlProfileDetail.TimberKind ?? string.Empty;
			TimberKindDescription = xmlProfileDetail.TimberKindDescription ?? string.Empty;
			FinishedWidth = xmlProfileDetail.FinishedWidth;
			CuttingWidth = xmlProfileDetail.CuttingWidth;
			PlaneWidth = xmlProfileDetail.PlaneWidth;
			FinishedThickness = xmlProfileDetail.FinishedThickness;
			CuttingThickness = xmlProfileDetail.CuttingThickness;
			PlaneThickness = xmlProfileDetail.PlaneThickness;
			CuttingsPercentage = xmlProfileDetail.CuttingsPercentage;
			ProfileUnit = xmlProfileDetail.ProfileUnit ?? string.Empty;
			CuttingLength = xmlProfileDetail.CuttingLength;
			SquareNr = xmlProfileDetail.SquareNr;
			SquareTimberKind = xmlProfileDetail.SquareTimberKind;
			SquareWidth = xmlProfileDetail.SquareWidth ?? 0;
			SquareThickness = xmlProfileDetail.SquareThickness ?? 0;
			SquareLength = xmlProfileDetail.SquareLength ?? 0;
			SquarePrice = xmlProfileDetail.SquarePrice ?? 0;
			Piece = xmlProfileDetail.Piece;
			FinishedLength = xmlProfileDetail.FinishedLength ?? 0m;
			WoodProfile = woodProfile;
		}
	}
}