using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Insiyab.Avalonia.Controls;

/// <summary>A rule with a word on it, the web's <c>.ins-divider</c>: the content sits between two lines, or after one line with the class <c>start</c>; with no content it is a single line. A plain rule is a <see cref="Separator"/>, and <c>Separator.v</c> the vertical one.</summary>
[PseudoClasses(EmptyPseudoClass)]
[TemplatePart("PART_Start", typeof(Control))]
[TemplatePart("PART_End", typeof(Control))]
public class Divider : ContentControl
{
    /// <summary>Set while the divider has nothing to show, so it closes to one line.</summary>
    public const string EmptyPseudoClass = ":empty";

    public Divider() => UpdateEmpty();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty)
            UpdateEmpty();
    }

    private void UpdateEmpty() => PseudoClasses.Set(EmptyPseudoClass, Content is null || Content is string { Length: 0 });
}
