using Avalonia;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a list hands down to its rows and a row to its parts: selected, display-only, master, ruled, compact, past, and a mark in flight.</summary>
internal static class ListLook
{
    /// <summary>Set by a selected row: its title takes the brand colour.</summary>
    public static readonly AttachedProperty<bool> SelectedProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Selected", typeof(ListLook), inherits: true);

    /// <summary>Set by a display-only list: its rows keep the plain title colour even when selected.</summary>
    public static readonly AttachedProperty<bool> StaticProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Static", typeof(ListLook), inherits: true);

    /// <summary>Set by a master list: its rows drop the chevron and keep a tinted, marked selection.</summary>
    public static readonly AttachedProperty<bool> MasterProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Master", typeof(ListLook), inherits: true);

    /// <summary>Set by a ruled list: its rows' dividers never step aside.</summary>
    public static readonly AttachedProperty<bool> RuledProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Ruled", typeof(ListLook), inherits: true);

    /// <summary>Set by a compact list: short rows with no dividers.</summary>
    public static readonly AttachedProperty<bool> CompactProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Compact", typeof(ListLook), inherits: true);

    /// <summary>Set by a ruled or compact list, so the rows of a plain list skip their rules.</summary>
    public static readonly AttachedProperty<bool> StyledProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Styled", typeof(ListLook), inherits: true);

    /// <summary>Set by a past row: its text is quieter.</summary>
    public static readonly AttachedProperty<bool> PastProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Past", typeof(ListLook), inherits: true);

    /// <summary>Set while a list's selection mark flies: the rows' own marks wait for it.</summary>
    public static readonly AttachedProperty<bool> MovingProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Moving", typeof(ListLook), inherits: true);

    public static bool GetSelected(StyledElement element) => element.GetValue(SelectedProperty);
    public static void SetSelected(StyledElement element, bool value) => element.SetValue(SelectedProperty, value);
    public static bool GetStatic(StyledElement element) => element.GetValue(StaticProperty);
    public static void SetStatic(StyledElement element, bool value) => element.SetValue(StaticProperty, value);
    public static bool GetMaster(StyledElement element) => element.GetValue(MasterProperty);
    public static void SetMaster(StyledElement element, bool value) => element.SetValue(MasterProperty, value);
    public static bool GetRuled(StyledElement element) => element.GetValue(RuledProperty);
    public static void SetRuled(StyledElement element, bool value) => element.SetValue(RuledProperty, value);
    public static bool GetCompact(StyledElement element) => element.GetValue(CompactProperty);
    public static void SetCompact(StyledElement element, bool value) => element.SetValue(CompactProperty, value);
    public static bool GetStyled(StyledElement element) => element.GetValue(StyledProperty);
    public static void SetStyled(StyledElement element, bool value) => element.SetValue(StyledProperty, value);
    public static bool GetPast(StyledElement element) => element.GetValue(PastProperty);
    public static void SetPast(StyledElement element, bool value) => element.SetValue(PastProperty, value);
    public static bool GetMoving(StyledElement element) => element.GetValue(MovingProperty);
    public static void SetMoving(StyledElement element, bool value) => element.SetValue(MovingProperty, value);
}
