namespace KG.MES.UI.Shared.Models;

/// <summary>
/// Политика скрытия кнопки в RowActions.
/// </summary>
public enum HidePolicy
{
    /// <summary>Всегда видима (например, «глазик»).</summary>
    Never,

    /// <summary>В drawer, если есть ещё хотя бы одна скрываемая кнопка (Auto или Always).</summary>
    Auto,

    /// <summary>Всегда в drawer (например, удаление).</summary>
    Always
}

/// <summary>
/// Описание действия в строке таблицы.
/// </summary>
public class RowAction
{
    public string Icon { get; set; } = "bi-question";
    public string Title { get; set; } = string.Empty;
    public string CssClass { get; set; } = "btn-outline-secondary";
    public Func<Task> OnClick { get; set; } = () => Task.CompletedTask;
    public HidePolicy HidePolicy { get; set; } = HidePolicy.Auto;
    public bool Visible { get; set; } = true;
}