using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A bottom sheet pulled down to close: by its handle or head (any pointer), or by its body once that is scrolled to the top (a finger). Past the threshold, or flung, it closes as its <see cref="Dialog.ClosedBy"/> allows; else it springs back.</summary>
internal sealed class SheetDrag
{
    // How far a pointer goes before the pull begins.
    private const double Start = 4;

    // A fling closes it from any distance.
    internal const double Fling = 900;

    // The most a sheet pulled upward follows the pointer.
    private const double Over = 12;

    private readonly Dialog _dialog;
    private IPointer? _pointer;
    private Point _from;
    private bool _body;
    private bool _dragging;
    private double _offset;
    private double _lastY;
    private ulong _lastTime;
    private double _velocity;

    public SheetDrag(Dialog dialog)
    {
        _dialog = dialog;
        dialog.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        dialog.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        dialog.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        dialog.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>The closing threshold for a sheet this tall: a third of it, never more than 140px.</summary>
    internal static double Threshold(double height) => Math.Min(140, Math.Max(48, height / 3));

    private DialogLayer? Layer() => DialogStack.Of(_dialog)?.Layers.FirstOrDefault(l => l.Dialog == _dialog);

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        _pointer = null;
        if (!_dialog.IsSheet || !_dialog.IsOpen || DialogStack.Of(_dialog) is not { } stack || !stack.IsTop(_dialog)) return;
        if (!e.GetCurrentPoint(_dialog).Properties.IsLeftButtonPressed) return;
        var source = e.Source as Visual;
        if (InGrip(source))
            _body = false;
        else if (e.Pointer.Type == PointerType.Touch && Body(source) is { } body && body.Offset.Y <= 0.5)
            _body = true;
        else return;
        if (TopLevel.GetTopLevel(_dialog) is not { } top) return;
        _pointer = e.Pointer;
        _from = e.GetPosition(top);
        _lastY = _from.Y;
        _lastTime = e.Timestamp;
        _velocity = 0;
        _dragging = false;
    }

    // The handle and the head (not a button in it) take any pointer.
    private bool InGrip(Visual? source)
    {
        for (var v = source; v is not null && v != _dialog; v = v.GetVisualParent())
        {
            if (v is Button) return false;
            if (v is Control { Name: "PART_Grip" or "PART_Head" } c && c.TemplatedParent == _dialog) return true;
        }
        return false;
    }

    private ScrollViewer? Body(Visual? source)
    {
        for (var v = source; v is not null && v != _dialog; v = v.GetVisualParent())
        {
            if (v is TextBox or Slider) return null;
            if (v is ScrollViewer { Name: "PART_Scroll" } s && s.TemplatedParent == _dialog) return s;
        }
        return null;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_pointer is null || e.Pointer != _pointer || TopLevel.GetTopLevel(_dialog) is not { } top) return;
        var at = e.GetPosition(top);
        var d = at - _from;
        if (!_dragging)
        {
            if (Math.Abs(d.X) < Start && Math.Abs(d.Y) < Start) return;
            // From the body only a pull down at the top takes over; a scroll stays a scroll.
            if (_body && (d.Y <= 0 || Math.Abs(d.X) > d.Y))
            {
                _pointer = null;
                return;
            }
            e.Pointer.Capture(_dialog);
            _dragging = true;
            _dialog.Transitions = null;
        }
        e.PreventGestureRecognition();
        e.Handled = true;
        var dt = e.Timestamp > _lastTime ? (e.Timestamp - _lastTime) / 1000.0 : 0;
        if (dt > 0) _velocity = (at.Y - _lastY) / dt;
        _lastY = at.Y;
        _lastTime = e.Timestamp;
        _offset = d.Y >= 0 ? d.Y : -Math.Min(Over, -d.Y * 0.2);
        _dialog.RenderTransform = Down(_offset);
        Layer()?.Hold(Math.Max(0, _offset) / Math.Max(1, _dialog.Bounds.Height));
    }

    private static TransformOperations Down(double y)
    {
        var b = TransformOperations.CreateBuilder(1);
        b.AppendTranslate(0, y);
        return b.Build();
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer is null || e.Pointer != _pointer) return;
        var dragging = _dragging;
        _pointer = null;
        _dragging = false;
        if (!dragging) return;
        e.Handled = true;
        e.Pointer.Capture(null);
        Let(_offset > Threshold(_dialog.Bounds.Height) || (_velocity > Fling && _offset > 0));
    }

    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_pointer is null || e.Pointer != _pointer || !_dragging || e.Source != _dialog) return;
        _pointer = null;
        _dragging = false;
        Let(false);
    }

    private void Let(bool close)
    {
        if (close && _dialog.RequestClose(outside: false)) return;
        Layer()?.SpringBack();
    }
}
