
using KG.MES.Shared.Models.Dto;
using Mapster;

namespace KG.MES.Shared.Models.ViewModels;

public class OrderCommentViewModel
{
	public Guid Id { get; set; }

	public string? Content { get; set; } = string.Empty;

	public DateTime? CreatedAt { get; set; }

	public DateTime? UpdatedAt { get; set; }

	public string? UserName { get; set; }

	/// <summary>
	/// Локальное состояние: новый (ещё не сохранён) или существующий.
	/// </summary>
	public bool IsNew { get; set; }

	/// <summary>
	/// Локальное состояние: редактируется ли сейчас.
	/// </summary>
	public bool IsEditing { get; set; }

	public OrderCommentViewModel() { }

	public OrderCommentViewModel(OrderCommentDto orderCommentDto)
	{
		orderCommentDto.Adapt(this);
	}
}