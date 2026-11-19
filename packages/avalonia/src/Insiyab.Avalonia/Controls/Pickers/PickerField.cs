using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>A field that opens a list to pick from (a project, a category, a month): one secondary button holding its <see cref="Label"/> at its start (quiet), the <see cref="Value"/> picked (cut with «…»), an optional <see cref="Detail"/> beside it (a code, dimmed; <see cref="IsDetailCode"/> keeps a code's figures and letters left to right), then a magnifier (a list with a search, <see cref="IsSearchable"/>) or a chevron, or a spinner while its choices are read (<see cref="IsBusy"/>). It is a button: its <c>Click</c> opens the list (a card hanging from it). Stretches to its column; sizes as a button does (a touch host's sizes too).</summary>
public class PickerField : Button
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<PickerField, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<PickerField, string?>(nameof(Value));

    public static readonly StyledProperty<string?> DetailProperty =
        AvaloniaProperty.Register<PickerField, string?>(nameof(Detail));

    public static readonly StyledProperty<bool> IsDetailCodeProperty =
        AvaloniaProperty.Register<PickerField, bool>(nameof(IsDetailCode));

    public static readonly StyledProperty<bool> IsValueCodeProperty =
        AvaloniaProperty.Register<PickerField, bool>(nameof(IsValueCode));

    public static readonly StyledProperty<bool> IsSearchableProperty =
        AvaloniaProperty.Register<PickerField, bool>(nameof(IsSearchable), true);

    public static readonly StyledProperty<bool> IsBusyProperty =
        AvaloniaProperty.Register<PickerField, bool>(nameof(IsBusy));

    // Lucide's search and chevron-down, as the library's other fields draw them
    private static readonly Geometry SearchGlyph = Geometry.Parse("M21 21l-4.34-4.34M3,11 A8,8 0 1 0 19,11 A8,8 0 1 0 3,11 Z");
    private static readonly Geometry ChevronGlyph = Geometry.Parse("m6 9 6 6 6-6");

    private readonly TextBlock _label = new() { Classes = { "muted" }, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _value = new() { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _detail = new() { Classes = { "muted" }, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
    private readonly Icon _glyph = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly LoadingSpinner _spinner = new() { Classes = { "sm" }, VerticalAlignment = VerticalAlignment.Center, IsVisible = false };

    protected override Type StyleKeyOverride => typeof(Button);

    public PickerField()
    {
        Classes.Add("secondary");
        Classes.Add("picker-field");
        // a field fills its column (a page may still set its own alignment)
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"), ColumnSpacing = 8 };
        grid.Bind(Grid.ColumnSpacingProperty, grid.GetResourceObservable("InsSp2"));
        Grid.SetColumn(_value, 1);
        Grid.SetColumn(_detail, 2);
        Grid.SetColumn(_glyph, 3);
        Grid.SetColumn(_spinner, 3);
        grid.Children.AddRange([_label, _value, _detail, _glyph, _spinner]);
        Content = grid;
        Show();
    }

    /// <summary>What the field picks («المشروع»), quiet at its start; also its name for a screen reader while none is set.</summary>
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>The choice shown (or a placeholder while none is picked).</summary>
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>A second, dimmed word beside the value: a code, or the other language's name. Hidden while empty.</summary>
    public string? Detail { get => GetValue(DetailProperty); set => SetValue(DetailProperty, value); }

    /// <summary>The detail is a code: its letters and figures kept left to right.</summary>
    public bool IsDetailCode { get => GetValue(IsDetailCodeProperty); set => SetValue(IsDetailCodeProperty, value); }

    /// <summary>The value is a code (a project's «P25»): kept left to right.</summary>
    public bool IsValueCode { get => GetValue(IsValueCodeProperty); set => SetValue(IsValueCodeProperty, value); }

    /// <summary>The list it opens has a search: a magnifier at its end (true, the default), else a chevron.</summary>
    public bool IsSearchable { get => GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }

    /// <summary>Its choices are being read: a spinner in the glyph's place.</summary>
    public bool IsBusy { get => GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LabelProperty || change.Property == ValueProperty || change.Property == DetailProperty || change.Property == IsDetailCodeProperty
            || change.Property == IsValueCodeProperty || change.Property == IsSearchableProperty || change.Property == IsBusyProperty)
            Show();
    }

    private void Show()
    {
        _label.Text = Label;
        _label.IsVisible = !string.IsNullOrEmpty(Label);
        _value.Text = Value;
        _value.Classes.Set("num", IsValueCode);
        _detail.Text = Detail;
        _detail.IsVisible = !string.IsNullOrEmpty(Detail);
        _detail.Classes.Set("num", IsDetailCode);
        _glyph.Data = IsSearchable ? SearchGlyph : ChevronGlyph;
        _glyph.IsVisible = !IsBusy;
        _spinner.IsVisible = IsBusy;
        if (Label is { Length: > 0 } label && global::Avalonia.Automation.AutomationProperties.GetName(this) is null or "")
            global::Avalonia.Automation.AutomationProperties.SetName(this, label);
    }
}
