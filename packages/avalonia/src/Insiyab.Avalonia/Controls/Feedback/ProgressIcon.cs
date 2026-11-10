using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Insiyab.Avalonia.Controls;

/// <summary>An icon (its content) with work running for it: <see cref="Value"/> 0 to 100 shrinks it inside a thin ring that fills, <see cref="IsIndeterminate"/> turns an arc there, <c>NaN</c> shows the icon alone; the ring takes no room.</summary>
[TemplatePart("PART_Ring", typeof(ProgressRing))]
[PseudoClasses(":progress", ":waiting", ":busy")]
public class ProgressIcon : ContentControl
{
    /// <summary>How far the work has come, 0 to 100; <c>NaN</c> (the default) shows no ring.</summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ProgressIcon, double>(nameof(Value), double.NaN);

    /// <summary>The work waits to start, its share not known: a turning arc in the ring's place, whatever <see cref="Value"/> says.</summary>
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressIcon, bool>(nameof(IsIndeterminate));

    private ProgressRing? _ring;

    /// <summary>How far the work has come, 0 to 100 (clamped); <c>NaN</c> (the default) shows no ring.</summary>
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The work waits to start: a turning arc in the ring's place.</summary>
    public bool IsIndeterminate { get => GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }

    /// <summary>Whether the ring with a fill shows: not waiting, and <see cref="Value"/> is a number.</summary>
    internal bool ShowsRing => !IsIndeterminate && double.IsFinite(Value);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _ring = e.NameScope.Find<ProgressRing>("PART_Ring");
        Fill();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == IsIndeterminateProperty) Fill();
    }

    // The ring takes a number only: on NaN it keeps its last fill, so it fades out as it was.
    private void Fill()
    {
        var ring = ShowsRing;
        PseudoClasses.Set(":progress", ring);
        PseudoClasses.Set(":waiting", IsIndeterminate);
        PseudoClasses.Set(":busy", ring || IsIndeterminate);
        if (_ring is not null && double.IsFinite(Value)) _ring.Value = Math.Clamp(Value, 0, 100);
    }
}
