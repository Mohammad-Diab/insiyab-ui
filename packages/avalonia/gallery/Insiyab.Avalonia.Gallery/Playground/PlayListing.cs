using Avalonia;
using Avalonia.Automation;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Input.Platform;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Gallery.Playground;

/// <summary>A code listing with buttons that wrap its long lines and copy it: a whole box on its own, or with the <c>attached</c> class the foot of an example or a playground.</summary>
public class PlayListing : Border
{
    public static readonly StyledProperty<string?> CodeProperty =
        AvaloniaProperty.Register<PlayListing, string?>(nameof(Code));

    private readonly TextBlock _text = new();
    private readonly Button _copy = new() { Content = "نسخ", [AutomationProperties.NameProperty] = "نسخ الشيفرة" };
    private readonly Button _wrap = new() { Content = "التفاف", [AutomationProperties.NameProperty] = "التفاف الأسطر الطويلة" };
    private readonly ScrollViewer _scroll;
    private StackPanel? _lines;
    private IDisposable? _done;

    // The web's .dx-code pre (26rem): a longer listing scrolls inside itself while the copy button stays in its corner.
    public const double MaxCodeHeight = 416;

    protected override Type StyleKeyOverride => typeof(Border);

    public PlayListing()
    {
        Classes.Add("play-listing");
        _text.Classes.Add("play-code");
        _copy.Classes.AddRange(["ghost", "sm", "play-copy"]);
        _copy.Click += async (_, _) => await Copy();
        _wrap.Classes.AddRange(["ghost", "sm", "play-copy", "play-wrap"]);
        _wrap.Click += (_, _) => SetWrapped(!IsWrapped);
        _scroll = new ScrollViewer
        {
            Content = _text,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = MaxCodeHeight,
            FlowDirection = FlowDirection.LeftToRight,
        };
        var tools = new StackPanel { Classes = { "play-tools" }, Orientation = Orientation.Horizontal, Children = { _wrap, _copy } };
        Child = new Panel { FlowDirection = FlowDirection.LeftToRight, Children = { _scroll, tools } };
    }

    public string? Code { get => GetValue(CodeProperty); set => SetValue(CodeProperty, value); }

    /// <summary>Whether this listing wraps its long lines; off by default, when they scroll sideways.</summary>
    public bool IsWrapped { get; private set; }

    public void SetWrapped(bool on)
    {
        IsWrapped = on;
        _wrap.Classes.Set("on", on);
        AutomationProperties.SetItemStatus(_wrap, on ? "مفعّل" : "");
        _scroll.HorizontalScrollBarVisibility = on ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        _scroll.Content = on ? _lines ??= Lines() : _text;
    }

    // Each line on its own, its leading spaces in a column of their own, so a long line wraps under its own indentation.
    private StackPanel Lines()
    {
        var all = new TextBlock();
        PlayCode.Highlight(all, Code ?? "");
        var lines = new List<List<Run>> { new() };
        foreach (var run in all.Inlines!.OfType<Run>())
        {
            var parts = (run.Text ?? "").Split('\n');
            for (var i = 0; i < parts.Length; i++)
            {
                if (i > 0) lines.Add([]);
                var text = parts[i].TrimEnd('\r');
                if (text.Length == 0) continue;
                var piece = new Run(text);
                piece.Classes.AddRange(run.Classes);
                lines[^1].Add(piece);
            }
        }
        var panel = new StackPanel { Classes = { "play-lines" } };
        foreach (var line in lines)
        {
            var indent = "";
            while (line.Count > 0)
            {
                var t = line[0].Text ?? "";
                var body = t.TrimStart(' ', '\t');
                indent += t[..(t.Length - body.Length)];
                if (body.Length > 0) { line[0].Text = body; break; }
                line.RemoveAt(0);
            }
            var rest = new TextBlock { Classes = { "play-line" }, TextWrapping = TextWrapping.Wrap, Inlines = [.. line] };
            Grid.SetColumn(rest, 1);
            panel.Children.Add(new Grid { ColumnDefinitions = new("Auto,*"), Children = { new TextBlock { Classes = { "play-line" }, Text = indent }, rest } });
        }
        return panel;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != CodeProperty) return;
        PlayCode.Highlight(_text, Code ?? "");
        _lines = null;
        if (IsWrapped) _scroll.Content = _lines = Lines();
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
