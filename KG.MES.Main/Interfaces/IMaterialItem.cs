using KG.MES.Main.Models;

namespace KG.MES.Main.Interfaces
{
	public interface IMaterialItem
	{
		string? ArticleNumber { get; }
		string? Description { get; }
		decimal Price { get; }
		decimal Quantity { get; }
		int Pieces { get; }
		string? Unit { get; }
		AccessoryType Type { get; }

		decimal TotalQuantity 
		{ 
			get
			{
				if (Quantity > 0 && Pieces > 0)
				{
					return Quantity * Pieces;
				}
				else if (Quantity > 0)
				{
					return Quantity;
				}
				else
				{
					return Pieces;
				}
			} 
		}
	}
}