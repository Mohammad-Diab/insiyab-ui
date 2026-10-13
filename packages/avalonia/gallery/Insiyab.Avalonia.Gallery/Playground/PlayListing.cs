using Avalonia;
using Avalonia.Automation;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Gallery.Playground;

/// <summary>A code listing under an example or a playground, with a button that copies it.</summary>
public class PlayListing : Border
{
    public static readonly StyledProperty<string?> CodeProperty =
        AvaloniaProperty.Register<PlayListing, string?>(nameof(Code));

    private readonly TextBlock _text = new();
    private readonly Button _copy = new() { Content = "نسخ", [AutomationProperties.NameProperty] = "نسخ الشيفرة" };
    private IDisposable? _done;

    protected override Type StyleKeyOverride => typeof(Border);

    public PlayListing()
    {
        Classes.Add("play-listing");
        _text.Classes.Add("play-code");
        _copy.Classes.AddRange(["ghost", "sm", "play-copy"]);
        _copy.Click += async (_, _) => await Copy();
        var scroll = new ScrollViewer
        {
            Content = _text,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FlowDirection = FlowDirection.LeftToRight,
        };
        Child = new Panel { FlowDirection = FlowDirection.LeftToRight, Children = { scroll, _copy } };
    }

    public string? Code { get => GetValue(CodeProperty); set => SetValue(CodeProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CodeProperty) PlayCode.Highlight(_text, Code ?? "");
    }

    private async Task Copy()
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard) return;
        await clipboard.SetTextAsync(Code ?? "");
        _copy.Content = "نُسخ";
        _copy.Classes.Add("done");
        _done?.Dispose();
        _done = DispatcherTimer.RunOnce(() =>
        {
            _copy.Content = "نسخ";
            _copy.Classes.Remove("done");
        }, TimeSpan.FromMilliseconds(1400));
    }
}
