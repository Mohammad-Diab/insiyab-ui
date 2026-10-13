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

    protected override Type StyleKeyOverride => typeof(Border);

    public PlayExample()
    {
        _view.Classes.Add("play-example");
        Child = new StackPanel { Children = { _view, _listing } };
    }

    public string? Code { get => GetValue(CodeProperty); set => SetValue(CodeProperty, value); }

    public ExampleLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>False shows the example alone, as the web's <c>code="none"</c>.</summary>
    public bool ShowCode { get => GetValue(ShowCodeProperty); set => SetValue(ShowCodeProperty, value); }


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
        _listing.Code = code;
    }
}
