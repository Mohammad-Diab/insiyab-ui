using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Insiyab.Avalonia.Controls;

/// <summary>The TextBlock the library's control templates use for their own text. The app's <c>TextBlock.*</c> type styles (<c>h1</c>, <c>label</c>, <c>muted</c>…) never reach it, which spares each one dozens of style watchers; <c>:is(TextBlock)</c> styles still do.</summary>
internal class TemplateText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(TemplateText);

    /// <summary>Shows content that is not a control (a control shows as itself) as a <see cref="TemplateText"/> of its text, as a ContentPresenter's default shows a TextBlock.</summary>
    public static IDataTemplate Content { get; } = new FuncDataTemplate<object?>((data, _) => data as Control ?? new TemplateText { Text = data?.ToString() });
}
