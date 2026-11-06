using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>A picture in a dialog of its own: a fixed frame of half the window each way that the picture fits inside (never past its own size), zoom by the wheel about the pointer, + and − (or the buttons), 0 to fit and 1 for its own size, and a drag or the arrows to move it while it is larger than the frame. Esc, a press outside and the cross close it.</summary>
public static class PictureViewer
{
    /// <summary>On a button: the picture it opens; while null the button is no door at all (no press, no focus, no tip), so a press goes through to what is under it.</summary>
    public static readonly AttachedProperty<IImage?> SourceProperty =
        AvaloniaProperty.RegisterAttached<Button, IImage?>("Source", typeof(PictureViewer));

    /// <summary>On a button: the viewer's title, as the person's name under a photo.</summary>
    public static readonly AttachedProperty<string?> TitleProperty =
        AvaloniaProperty.RegisterAttached<Button, string?>("Title", typeof(PictureViewer));

    // The frame takes this share of the window each way; the dialog keeps this much room to the window's edges.
    private const double FrameShare = 0.5, WindowMargin = 96, Chrome = 24 * 2 + 2, SmallDialog = 416;

    static PictureViewer()
    {
        SourceProperty.Changed.AddClassHandler<Button>((button, _) => Follow(button));
        // A source bound to null from the start changes nothing, so the button is set up as its template comes.
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<Button>((button, _) =>
        {
            if (button.IsSet(SourceProperty)) Follow(button);
        });
        Button.ClickEvent.AddClassHandler(typeof(Button), OnClick, RoutingStrategies.Bubble);
    }

    public static IImage? GetSource(Button button) => button.GetValue(SourceProperty);
    public static void SetSource(Button button, IImage? value) => button.SetValue(SourceProperty, value);
    public static string? GetTitle(Button button) => button.GetValue(TitleProperty);
    public static void SetTitle(Button button, string? value) => button.SetValue(TitleProperty, value);

    private static readonly AttachedProperty<bool> WatchedProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("Watched", typeof(PictureViewer));

    private static void Follow(Button button)
    {
        var has = GetSource(button) is not null;
        if (has && !button.GetValue(WatchedProperty))
        {
            button.SetValue(WatchedProperty, true);
            button.AttachedToVisualTree += (_, _) => Follow(button);
        }
        button.SetCurrentValue(InputElement.IsHitTestVisibleProperty, has);
        button.SetCurrentValue(InputElement.FocusableProperty, has);
        var words = has ? Words.Of(button)["picture.title"] : null;
        button.SetCurrentValue(ToolTip.TipProperty, words);
        button.SetCurrentValue(AutomationProperties.NameProperty, words);
    }

    private static void OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || e.Source != button || GetSource(button) is not { } picture) return;
        e.Handled = true;
        _ = ShowAsync(button, picture, GetTitle(button));
    }

    /// <summary>Opens <paramref name="picture"/> over <paramref name="owner"/>'s window (in a single view too) and waits until it closes; focus goes back to <paramref name="owner"/>.</summary>
    public static Task ShowAsync(Control owner, IImage picture, string? title = null)
    {
        var words = Words.Of(owner);
        var room = TopLevel.GetTopLevel(owner)?.Bounds.Size ?? new Size(SmallDialog + 400, 720);
        var titled = !string.IsNullOrWhiteSpace(title);
        var view = new PictureZoom(picture) { Room = new Size(Math.Max(160, room.Width * FrameShare), Math.Max(120, room.Height * FrameShare)) };
        var frame = new Border { Child = view, ClipToBounds = true, HorizontalAlignment = HorizontalAlignment.Center };
        frame.Bind(Border.CornerRadiusProperty, frame.GetResourceObservable("InsRMd"));
        frame.Bind(Border.BackgroundProperty, frame.GetResourceObservable("InsWash1"));

        Button Tool(string icon, string tip, Action act)
        {
            var icon1 = new Icon();
            icon1.Bind(Icon.DataProperty, icon1.GetResourceObservable(icon));
            var b = new Button { Content = icon1, Classes = { "ghost", "icon", "sm" } };
            ToolTip.SetTip(b, tip);
            AutomationProperties.SetName(b, tip);
            b.Click += (_, _) => act();
            return b;
        }
        var zoomOut = Tool("InsPictureZoomOut", words["picture.zoomOut"], () => view.Step(-1));
        var zoomIn = Tool("InsPictureZoomIn", words["picture.zoomIn"], () => view.Step(1));
        var percent = new TextBlock { MinWidth = 48, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FlowDirection = FlowDirection.LeftToRight, Classes = { "muted", "num" } };
        var reset = new Button { Classes = { "secondary", "sm" }, VerticalAlignment = VerticalAlignment.Center };
        reset.Click += (_, _) =>
        {
            if (view.IsFitted) view.ShowActual();
            else view.Fit();
        };
        var tools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center, Children = { zoomOut, percent, zoomIn, reset } };

        var dialog = new Dialog
        {
            Title = titled ? title : null,
            Content = new StackPanel { Spacing = 12, Children = { frame, tools } },
            ClosedBy = DialogClosedBy.Any,
            GrowsFromAnchor = true,
            MaxHeight = Math.Max(704, room.Height - WindowMargin),
            MaxWidth = Math.Min(Math.Max(SmallDialog, view.Room.Width + Chrome), Math.Max(SmallDialog, room.Width - WindowMargin)),
        };
        AutomationProperties.SetName(dialog, titled ? title : words["picture.title"]);

        void Follow()
        {
            percent.Text = Math.Round(view.Zoom * 100).ToString(CultureInfo.InvariantCulture) + "%";
            zoomOut.IsEnabled = view.Zoom > view.MinZoom + 1e-6;
            zoomIn.IsEnabled = view.Zoom < view.MaxZoom - 1e-6;
            reset.Content = view.IsFitted ? words["picture.actual"] : words["picture.fit"];
            reset.IsEnabled = !view.IsFitted || view.MinZoom < 1 - 1e-6;
        }
        view.ZoomChanged += Follow;
        Follow();

        EventHandler<KeyEventArgs> keys = (_, e) =>
        {
            if (!dialog.IsOpen || (DialogStack.Of(dialog) is { } stack && !stack.IsTop(dialog))) return;
            if (!view.Press(e.Key)) return;
            e.Handled = true;
        };
        dialog.Opened += (_, _) => Dispatcher.UIThread.Post(() => view.Focus(), DispatcherPriority.Input);
        if (TopLevel.GetTopLevel(owner) is { } top)
        {
            top.AddHandler(InputElement.KeyDownEvent, keys, RoutingStrategies.Tunnel);
            dialog.Closed += (_, _) => top.RemoveHandler(InputElement.KeyDownEvent, keys);
        }
        return dialog.ShowAsync(owner);
    }
}

/// <summary>The viewer's frame of a fixed size <see cref="Room"/>: the picture drawn at <see cref="Zoom"/>, fitted at first and never above its own size, centred while smaller than the frame.</summary>
internal sealed class PictureZoom : Control
{
    private const double StepFactor = 1.25, ArrowStep = 48;

    private static Cursor? s_move;

    private readonly IImage _picture;
    private Size _room;
    private double _fit = 1, _zoom = 1;
    private Vector _offset;
    private Point? _dragFrom;
    private Vector _dragOffset;

    public PictureZoom(IImage picture)
    {
        _picture = picture;
        ClipToBounds = true;
        FlowDirection = FlowDirection.LeftToRight;
        Focusable = true;
        FocusAdorner = null;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    public event Action? ZoomChanged;

    /// <summary>The picture's size in pixels for a bitmap (its own size is one pixel a unit), else its size.</summary>
    internal Size PictureSize => _picture is Bitmap bitmap ? new Size(bitmap.PixelSize.Width, bitmap.PixelSize.Height) : _picture.Size;

    public Size Room
    {
        get => _room;
        set
        {
            _room = value;
            var size = PictureSize;
            _fit = size.Width <= 0 || size.Height <= 0 ? 1 : Math.Min(1, Math.Min(value.Width / size.Width, value.Height / size.Height));
            _zoom = _fit;
            _offset = default;
            InvalidateMeasure();
        }
    }

    public double Zoom => _zoom;

    public double MinZoom => _fit;

    public double MaxZoom => Math.Max(4, _fit * 4);

    public bool IsFitted => Math.Abs(_zoom - _fit) < 1e-6;

    internal Vector Offset => _offset;

    private Size Scaled => PictureSize * _zoom;

    public bool CanPan => Scaled.Width > _room.Width + 0.5 || Scaled.Height > _room.Height + 0.5;

    public void Step(int direction, Point? about = null) => ZoomTo(_zoom * Math.Pow(StepFactor, direction), about);

    public void Fit() => ZoomTo(_fit, null);

    public void ShowActual() => ZoomTo(1, null);

    public void PanBy(Vector delta)
    {
        _offset = Clamp(_offset + delta);
        InvalidateVisual();
    }

    /// <summary>The viewer's keys: + and − zoom, 0 fits, 1 shows the own size, the arrows move a zoomed picture the way they point on the screen; false for any other key.</summary>
    public bool Press(Key key)
    {
        switch (key)
        {
            case Key.OemPlus or Key.Add: Step(1); return true;
            case Key.OemMinus or Key.Subtract: Step(-1); return true;
            case Key.D0 or Key.NumPad0: Fit(); return true;
            case Key.D1 or Key.NumPad1: ShowActual(); return true;
            case Key.Left or Key.Right or Key.Up or Key.Down when CanPan:
                var step = ArrowStep * (key is Key.Left or Key.Up ? -1 : 1);
                PanBy(key is Key.Left or Key.Right ? new Vector(step, 0) : new Vector(0, step));
                return true;
            default:
                return false;
        }
    }

    /// <summary>Zooms keeping the picture's point under <paramref name="about"/> where it is (the frame's centre when null).</summary>
    public void ZoomTo(double zoom, Point? about)
    {
        zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        if (Math.Abs(zoom - _zoom) < 1e-9) return;
        var at = about ?? new Point(_room.Width / 2, _room.Height / 2);
        var point = (at - Origin()) / _zoom;
        _zoom = zoom;
        _offset = Clamp(new Vector(point.X * zoom - at.X, point.Y * zoom - at.Y));
        _dragFrom = null;
        Cursor = CanPan ? s_move ??= new Cursor(StandardCursorType.SizeAll) : null;
        InvalidateVisual();
        ZoomChanged?.Invoke();
    }

    private Vector Clamp(Vector offset) =>
        new(Math.Clamp(offset.X, 0, Math.Max(0, Scaled.Width - _room.Width)), Math.Clamp(offset.Y, 0, Math.Max(0, Scaled.Height - _room.Height)));

    // The picture's top-left in the frame: centred while smaller than the frame, else moved by the offset.
    internal Point Origin()
    {
        var s = Scaled;
        return new Point(s.Width <= _room.Width ? (_room.Width - s.Width) / 2 : -_offset.X, s.Height <= _room.Height ? (_room.Height - s.Height) / 2 : -_offset.Y);
    }

    protected override Size MeasureOverride(Size availableSize) => _room;

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        context.DrawImage(_picture, new Rect(PictureSize), new Rect(Origin(), Scaled));
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Delta.Y == 0) return;
        ZoomTo(_zoom * Math.Pow(StepFactor, Math.Sign(e.Delta.Y)), e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!CanPan || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragFrom = e.GetPosition(this);
        _dragOffset = _offset;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragFrom is not { } from) return;
        _offset = Clamp(_dragOffset - (e.GetPosition(this) - from));
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragFrom is null) return;
        _dragFrom = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _dragFrom = null;
    }
}
