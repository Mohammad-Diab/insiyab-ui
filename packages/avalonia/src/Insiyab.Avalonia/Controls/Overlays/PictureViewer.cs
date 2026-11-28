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

/// <summary>A picture in a dialog of its own. By default a fixed frame of half the window each way that the picture fits inside (never past its own size), zoom by the wheel about the pointer, + and − (or the buttons), 0 to fit and 1 for its own size, and a drag or the arrows to move it while it is larger than the frame. With fillWindow (or <see cref="FillWindowProperty"/> on a button) it takes the window's room instead (all of a phone's, less the dialog's edges), with no buttons: the picture fits the whole of it, a tap (a click) zooms in about the point tapped and a second one fits it again, two fingers pinch it about their middle, a finger or a drag moves it. Esc, a press outside and the cross close it.</summary>
public static class PictureViewer
{
    /// <summary>On a button: the picture it opens; while null the button is no door at all (no press, no focus, no tip), so a press goes through to what is under it.</summary>
    public static readonly AttachedProperty<IImage?> SourceProperty =
        AvaloniaProperty.RegisterAttached<Button, IImage?>("Source", typeof(PictureViewer));

    /// <summary>On a button: the viewer's title, as the person's name under a photo.</summary>
    public static readonly AttachedProperty<string?> TitleProperty =
        AvaloniaProperty.RegisterAttached<Button, string?>("Title", typeof(PictureViewer));

    /// <summary>On a button: open the picture over the window's whole room, zoomed by a tap or a pinch, instead of the framed viewer with its buttons.</summary>
    public static readonly AttachedProperty<bool> FillWindowProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("FillWindow", typeof(PictureViewer));

    // The framed viewer: the frame takes this share of the window each way, and the dialog keeps this much room to the window's edges.
    private const double FrameShare = 0.5, WindowMargin = 96, Chrome = 24 * 2 + 2, SmallDialog = 416;

    // The filling viewer: from this width the window is a desktop's, where the dialog keeps WindowMargin to its edges (a phone's dialog takes all the layer gives it).
    private const double WideFrom = 600;

    /// <summary>The small room kept between the filling viewer's picture and the dialog's edges.</summary>
    internal const double Gutter = 8;

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
    public static bool GetFillWindow(Button button) => button.GetValue(FillWindowProperty);
    public static void SetFillWindow(Button button, bool value) => button.SetValue(FillWindowProperty, value);

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
        _ = ShowAsync(button, picture, GetTitle(button), GetFillWindow(button));
    }

    /// <summary>Opens <paramref name="picture"/> over <paramref name="owner"/>'s window (in a single view too) and waits until it closes; focus goes back to <paramref name="owner"/>. <paramref name="fillWindow"/> takes the window's room with tap and pinch zoom instead of the framed viewer with its buttons.</summary>
    public static Task ShowAsync(Control owner, IImage picture, string? title = null, bool fillWindow = false) =>
        fillWindow ? ShowFilling(owner, picture, title) : ShowFramed(owner, picture, title);

    private static Task ShowFramed(Control owner, IImage picture, string? title)
    {
        var words = Words.Of(owner);
        var room = TopLevel.GetTopLevel(owner)?.Bounds.Size ?? new Size(SmallDialog + 400, 720);
        var titled = !string.IsNullOrWhiteSpace(title);
        var view = new PictureZoom(picture, new Size(Math.Max(160, room.Width * FrameShare), Math.Max(120, room.Height * FrameShare)));
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
        return Show(owner, dialog, view);
    }

    private static Task ShowFilling(Control owner, IImage picture, string? title)
    {
        var words = Words.Of(owner);
        var room = TopLevel.GetTopLevel(owner)?.Bounds.Size ?? new Size(1024, 768);
        var titled = !string.IsNullOrWhiteSpace(title);
        var wide = room.Width >= WideFrom;
        var view = new PictureZoom(picture);
        AutomationProperties.SetName(view, titled ? title : words["picture.title"]);

        var dialog = new Dialog
        {
            // with no words of its own the head is still drawn (an empty title), for its cross
            Title = titled ? title : "",
            Content = view,
            Classes = { "picture" },
            Padding = new Thickness(Gutter),
            VerticalContentAlignment = VerticalAlignment.Stretch,
            ClosedBy = DialogClosedBy.Any,
            GrowsFromAnchor = true,
            MaxWidth = wide ? Math.Max(SmallDialog, room.Width - WindowMargin) : double.PositiveInfinity,
            MaxHeight = wide ? Math.Max(SmallDialog, room.Height - WindowMargin) : double.PositiveInfinity,
        };
        AutomationProperties.SetName(dialog, titled ? title : words["picture.title"]);
        return Show(owner, dialog, view);
    }

    // the viewer's keys go to the picture while its dialog is the top one, and the picture takes the focus as it opens
    private static Task Show(Control owner, Dialog dialog, PictureZoom view)
    {
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

/// <summary>The viewer's picture. Framed (given a fixed <see cref="Room"/>): fitted at first, never above its own size, centred while smaller than the frame, moved by a drag while larger. Else as large as the room the layout gives: fitted at first, the largest it can be inside that room (a small picture grows, up to <see cref="FitMost"/> times its size), and centred while smaller than the room. A tap toggles between fitted and zoomed in about the point tapped; a pinch zooms about the fingers' middle and moves with it; one finger or a mouse moves a picture larger than the room, to every edge of it.</summary>
internal sealed class PictureZoom : Control
{
    private const double StepFactor = 1.25, ArrowStep = 48;

    /// <summary>A fitted picture grows to at most this many times its own size (a tiny picture would only blur past it).</summary>
    internal const double FitMost = 4;

    /// <summary>A tap zooms to this many times the fitted size (and at least to the picture's own size).</summary>
    internal const double TapZoom = 2.5;

    private static Cursor? s_move, s_hand;

    private readonly IImage _picture;
    private readonly bool _framed;
    private Size _room;
    private double _fit = 1, _zoom = 1;
    private Vector _offset;

    // the press that may be a tap or a drag, the fingers down, and a pinch under way (the fingers' distance, the zoom and the picture's point under their middle as it started)
    private IPointer? _press;
    private Point _pressAt, _dragFrom;
    private Vector _dragOffset;
    private bool _moved, _multi;
    private readonly Dictionary<IPointer, Point> _touches = [];
    private (double Distance, double Zoom, Vector Under)? _pinch;

    public PictureZoom(IImage picture, Size? frame = null)
    {
        _picture = picture;
        _framed = frame is not null;
        if (frame is { } room) UseRoom(room);
        ClipToBounds = true;
        FlowDirection = FlowDirection.LeftToRight;
        Focusable = true;
        FocusAdorner = null;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
        ShowCursor();
    }

    public event Action? ZoomChanged;

    /// <summary>The picture's size in pixels for a bitmap (its own size is one pixel a unit), else its size.</summary>
    internal Size PictureSize => _picture is Bitmap bitmap ? new Size(bitmap.PixelSize.Width, bitmap.PixelSize.Height) : _picture.Size;

    /// <summary>The room the picture is shown in: the frame's fixed size, or the size the layout gave it.</summary>
    public Size Room => _room;

    public double Zoom => _zoom;

    public double MinZoom => _fit;

    public double MaxZoom => Math.Max(4, _fit * 4);

    public bool IsFitted => Math.Abs(_zoom - _fit) < 1e-6;

    /// <summary>The zoom a tap goes to from the fitted picture.</summary>
    internal double Zoomed => Math.Min(MaxZoom, Math.Max(1, _fit * TapZoom));

    internal Vector Offset => _offset;

    private Size Scaled => PictureSize * _zoom;

    public bool CanPan => Scaled.Width > _room.Width + 0.5 || Scaled.Height > _room.Height + 0.5;

    // the room changed (laid out, the window resized): a fitted picture fits the new room, a zoomed one keeps its zoom where it can
    private void UseRoom(Size room)
    {
        if (room == _room) return;
        var fitted = _room == default || IsFitted;
        _room = room;
        var size = PictureSize;
        _fit = size.Width <= 0 || size.Height <= 0 || room.Width <= 0 || room.Height <= 0
            ? 1
            : Math.Min(_framed ? 1 : FitMost, Math.Min(room.Width / size.Width, room.Height / size.Height));
        if (fitted)
        {
            _zoom = _fit;
            _offset = default;
        }
        else
        {
            _zoom = Math.Clamp(_zoom, MinZoom, MaxZoom);
            _offset = Clamp(_offset);
        }
        ShowCursor();
        InvalidateVisual();
        ZoomChanged?.Invoke();
    }

    public void Step(int direction, Point? about = null) => ZoomTo(_zoom * Math.Pow(StepFactor, direction), about);

    public void Fit() => ZoomTo(_fit, null);

    public void ShowActual() => ZoomTo(1, null);

    /// <summary>A tap: fitted, it zooms in about <paramref name="at"/>; else it fits again.</summary>
    public void Toggle(Point at)
    {
        if (IsFitted) ZoomTo(Zoomed, at);
        else Fit();
    }

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

    /// <summary>Zooms keeping the picture's point under <paramref name="about"/> where it is (the room's centre when null).</summary>
    public void ZoomTo(double zoom, Point? about)
    {
        zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        if (Math.Abs(zoom - _zoom) < 1e-9) return;
        var at = about ?? new Point(_room.Width / 2, _room.Height / 2);
        var point = (at - Origin()) / _zoom;
        Place(zoom, point, at);
    }

    // the zoom set with the picture's point under the screen's point (as near as the edges let it)
    private void Place(double zoom, Vector picturePoint, Point at)
    {
        _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        _offset = Clamp(new Vector(picturePoint.X * _zoom - at.X, picturePoint.Y * _zoom - at.Y));
        ShowCursor();
        InvalidateVisual();
        ZoomChanged?.Invoke();
    }

    private void ShowCursor() => Cursor = CanPan ? s_move ??= new Cursor(StandardCursorType.SizeAll) : _framed ? null : s_hand ??= new Cursor(StandardCursorType.Hand);

    private Vector Clamp(Vector offset) =>
        new(Math.Clamp(offset.X, 0, Math.Max(0, Scaled.Width - _room.Width)), Math.Clamp(offset.Y, 0, Math.Max(0, Scaled.Height - _room.Height)));

    // The picture's top-left in the room: centred while smaller than the room, else moved by the offset.
    internal Point Origin()
    {
        var s = Scaled;
        return new Point(s.Width <= _room.Width ? (_room.Width - s.Width) / 2 : -_offset.X, s.Height <= _room.Height ? (_room.Height - s.Height) / 2 : -_offset.Y);
    }

    // the whole room the layout gives (the viewer's body does not scroll); measured with no bound, the picture's own size
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_framed) return _room;
        var own = PictureSize;
        return new Size(double.IsInfinity(availableSize.Width) ? own.Width : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? own.Height : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!_framed) UseRoom(finalSize);
        return finalSize;
    }

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
        var touch = e.Pointer.Type == PointerType.Touch;
        if (!touch && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        // framed, a press only moves a picture larger than the frame (no tap zoom)
        if (_framed && !touch && !CanPan) return;
        var at = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;
        if (touch)
        {
            _touches[e.Pointer] = at;
            if (_touches.Count >= 2)
            {
                StartPinch();
                return;
            }
        }
        else if (e.ClickCount > 1 && !_framed)
        {
            _press = null; // a double click's second press: the first one's tap already zoomed
            return;
        }
        _press = e.Pointer;
        _pressAt = _dragFrom = at;
        _dragOffset = _offset;
        _moved = _framed;
        _multi = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var at = e.GetPosition(this);
        if (_touches.ContainsKey(e.Pointer))
        {
            _touches[e.Pointer] = at;
            e.PreventGestureRecognition();
            if (_pinch is not null)
            {
                FollowPinch();
                e.Handled = true;
                return;
            }
        }
        if (!ReferenceEquals(e.Pointer, _press)) return;
        if (!_moved && Point.Distance(at, _pressAt) > Touch.Slop) _moved = true;
        if (_moved && CanPan)
        {
            _offset = Clamp(_dragOffset - (at - _dragFrom));
            InvalidateVisual();
        }
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var at = e.GetPosition(this);
        var tracked = _touches.Remove(e.Pointer) || ReferenceEquals(e.Pointer, _press);
        if (_pinch is not null && _touches.Count < 2)
        {
            _pinch = null;
            // the finger left down goes on moving the picture from where it is
            if (_touches.Count == 1)
            {
                var (pointer, point) = _touches.First();
                _press = pointer;
                _pressAt = _dragFrom = point;
                _dragOffset = _offset;
                _moved = true;
            }
        }
        if (ReferenceEquals(e.Pointer, _press))
        {
            _press = null;
            if (!_moved && !_multi) Toggle(at);
        }
        if (_touches.Count == 0) _multi = false;
        if (ReferenceEquals(e.Pointer.Captured, this)) e.Pointer.Capture(null);
        if (tracked) e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _touches.Remove(e.Pointer);
        if (_touches.Count < 2) _pinch = null;
        if (ReferenceEquals(e.Pointer, _press)) _press = null;
        if (_touches.Count == 0) _multi = false;
    }

    private void StartPinch()
    {
        var (a, b) = TwoFingers();
        var distance = Point.Distance(a, b);
        if (distance < 1) return;
        var middle = (a + b) / 2;
        _pinch = (distance, _zoom, (middle - Origin()) / _zoom);
        _press = null;
        _multi = true;
    }

    private void FollowPinch()
    {
        if (_pinch is not { } pinch) return;
        var (a, b) = TwoFingers();
        var distance = Point.Distance(a, b);
        if (distance < 1) return;
        Place(pinch.Zoom * distance / pinch.Distance, pinch.Under, (a + b) / 2);
    }

    private (Point A, Point B) TwoFingers()
    {
        using var e = _touches.Values.GetEnumerator();
        e.MoveNext();
        var a = e.Current;
        e.MoveNext();
        return (a, e.Current);
    }
}
