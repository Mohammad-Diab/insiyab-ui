using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Makes Avalonia's DatePicker and TimePicker drums close like every popover: it opens below the field, so a second click on the field closes it, and Escape closes it.</summary>
public static class DrumPopover
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TemplatedControl, bool>("IsEnabled", typeof(DrumPopover));

    static DrumPopover()
    {
        Button.ClickEvent.AddClassHandler<DatePicker>(OnClick, handledEventsToo: true);
        Button.ClickEvent.AddClassHandler<TimePicker>(OnClick, handledEventsToo: true);
        InputElement.KeyDownEvent.AddClassHandler<DatePicker>(OnKeyDown);
        InputElement.KeyDownEvent.AddClassHandler<TimePicker>(OnKeyDown);
    }

    public static bool GetIsEnabled(TemplatedControl element) => element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(TemplatedControl element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static Popup? Pop(TemplatedControl picker) =>
        picker.GetVisualDescendants().OfType<Popup>().FirstOrDefault(p => p.Name == "PART_Popup" && p.TemplatedParent == picker);

    // Avalonia centres the drum over the field, covering it; by the time the click bubbles here the picker has opened it, so this puts it against the template's anchor, which a flip keeps clear of the field too.
    private static void OnClick(TemplatedControl picker, RoutedEventArgs e)
    {
        if (!GetIsEnabled(picker) || e.Source is not Button { Name: "PART_FlyoutButton" } || Pop(picker) is not { IsOpen: true } pop) return;
        pop.PlacementConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipY | PopupPositionerConstraintAdjustment.SlideX | PopupPositionerConstraintAdjustment.SlideY;
        pop.VerticalOffset = 0;
    }

    private static void OnKeyDown(TemplatedControl picker, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || !GetIsEnabled(picker) || Pop(picker) is not { IsOpen: true } pop) return;
        pop.IsOpen = false;
        e.Handled = true;
    }
}
