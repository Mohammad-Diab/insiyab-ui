using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

// A picker's pop-up: the menus unit's pop placement, room and entrance, in the TopLevel's overlay layer, without taking focus.
internal sealed class PickerFlyout : Flyout
{
    public PickerFlyout()
    {
        Pop.Init(this);
        FlyoutPresenterClasses.Add("pop");
        FlyoutPresenterClasses.Add("picker");
        ShowMode = FlyoutShowMode.Transient;
    }

    public bool IsEnd { get; set; }
}
