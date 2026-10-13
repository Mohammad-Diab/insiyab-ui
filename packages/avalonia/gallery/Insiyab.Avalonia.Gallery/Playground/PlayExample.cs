using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Playground;

public enum ExampleLayout { Row, Stack }

/// <summary>A page's main example: the code rendered as it is written, then its listing; the web's <c>&lt;demo-example&gt;</c>.</summary>
public class PlayExample : Border
{
    public static readonly StyledProperty<string?> CodeProperty =
        AvaloniaProperty.Register<PlayExample, string?>(nameof(Code));

    public static readonly StyledProperty<ExampleLayout> LayoutProperty =
        AvaloniaProperty.Register<PlayExample, ExampleLayout>(nameof(Layout));

    public static readonly StyledProperty<bool> ShowCodeProperty =
        AvaloniaProperty.Register<PlayExample, bool>(nameof(ShowCode), true);

    private readonly Border _view = new();
    private readonly PlayListing _listing = new();
    private Action<Control>? _stage;

    protected override Type StyleKeyOverride => typeof(Border);

    public PlayExample()
    {
        _view.Classes.Add("play-example");
        _listing.Classes.Add("attached");
        Child = new StackPanel { Children = { _view, _listing } };
    }

    public string? Code { get => GetValue(CodeProperty); set => SetValue(CodeProperty, value); }

    public ExampleLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>False shows the example alone, as the web's <c>code="none"</c>.</summary>
    public bool ShowCode { get => GetValue(ShowCodeProperty); set => SetValue(ShowCodeProperty, value); }

    /// <summary>Wires the rendered example each time it renders, as a page's code-behind would: events, sample data. Given the example's panel.</summary>
    public Action<Control>? Stage
    {
        get => _stage;
        set
        {
            _stage = value;
            if (_view.Child is Panel panel) value?.Invoke(panel);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowCodeProperty)
        {
            _listing.IsVisible = ShowCode;
            _view.Classes.Set("solo", !ShowCode);
        }
        if (change.Property != CodeProperty && change.Property != LayoutProperty) return;
        var code = Code ?? "";
        _view.Child = Layout == ExampleLayout.Row
            ? PlayCode.Render(code, "WrapPanel", "ItemSpacing=\"10\" LineSpacing=\"10\"")
            : PlayCode.Render(code, "StackPanel", "Spacing=\"12\"");
        if (_view.Child is Panel panel) _stage?.Invoke(panel);
        _listing.Code = code;
    }
}
