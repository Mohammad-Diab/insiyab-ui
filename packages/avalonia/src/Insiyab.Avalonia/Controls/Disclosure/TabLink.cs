using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>A tab that goes somewhere instead of showing a panel: the web's <c>&lt;a class="ins-tab" href&gt;</c>. It looks like a tab, is never selected, raises <see cref="Click"/>, and wears the selected look while <see cref="IsCurrent"/> (the page it points at is the one showing).</summary>
[PseudoClasses(":current", ":link")]
public class TabLink : TabItem
{
    public static readonly StyledProperty<bool> IsCurrentProperty =
        AvaloniaProperty.Register<TabLink, bool>(nameof(IsCurrent));

    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<TabLink, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private bool _pressed;

    static TabLink()
    {
        IsCurrentProperty.Changed.AddClassHandler<TabLink>((x, e) => x.PseudoClasses.Set(":current", e.GetNewValue<bool>()));
    }

    public TabLink() => PseudoClasses.Add(":link");

    protected override Type StyleKeyOverride => typeof(TabItem);

    public bool IsCurrent { get => GetValue(IsCurrentProperty); set => SetValue(IsCurrentProperty, value); }

    public event EventHandler<RoutedEventArgs>? Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _pressed = true;
        PseudoClasses.Set(":pressed", true);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!_pressed) return;
        _pressed = false;
        PseudoClasses.Set(":pressed", false);
        e.Handled = true;
        if (new Rect(Bounds.Size).Contains(e.GetPosition(this))) RaiseEvent(new RoutedEventArgs(ClickEvent));
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pressed = false;
        PseudoClasses.Set(":pressed", false);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space && !e.Handled)
        {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(ClickEvent));
            return;
        }
        base.OnKeyDown(e);
    }
}
