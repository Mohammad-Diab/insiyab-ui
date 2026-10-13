using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

// A picker's pop-up: the menus unit's pop placement, room and entrance, in the TopLevel's overlay layer, without taking focus.
internal sealed class PickerFlyout : Flyout
{
    private bool _isEnd;

    public PickerFlyout()
    {
        Pop.Init(this);
        FlyoutPresenterClasses.Add("pop");
        FlyoutPresenterClasses.Add("picker");
        ShowMode = FlyoutShowMode.Transient;
    }

    public bool IsEnd
    {
        get => _isEnd;
        set
        {
            _isEnd = value;
            Pop.Align(this, value);
        }
    }
}
