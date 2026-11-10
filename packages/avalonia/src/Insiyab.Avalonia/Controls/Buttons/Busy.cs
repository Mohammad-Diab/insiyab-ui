using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A busy state for any button: <c>ins:Busy.IsBusy</c> turns its leading <see cref="Icon"/> into the small <see cref="LoadingSpinner"/> (with no icon, the content fades for it), keeps its size, and by default holds it disabled under its own <c>IsEnabled</c>; <c>:busy</c> for styles.</summary>
public static class Busy
{
    /// <summary>The pseudo-class a busy button carries.</summary>
    public const string BusyPseudoClass = ":busy";

    // On the button whose template turns the spinner in place of its content: the busy button had no icon to turn.
    internal const string ContentPseudoClass = ":busy-content";

    /// <summary>True while the button's work runs: its leading icon turns, and (by default) it cannot be pressed. False by default.</summary>
    public static readonly AttachedProperty<bool> IsBusyProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsBusy", typeof(Busy));

    /// <summary>Whether busy also disables the button. True by default; false keeps it pressable, for work a second press should reach (as a "stop").</summary>
    public static readonly AttachedProperty<bool> DisablesProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Disables", typeof(Busy), true);

    private sealed class State
    {
        public IDisposable? Off;
        public Icon? Icon;
        public Control? Face;
        public bool Waiting;
    }

    private static readonly ConditionalWeakTable<Control, State> s_states = new();

    static Busy()
    {
        IsBusyProperty.Changed.AddClassHandler<Control>((c, _) => Update(c));
        DisablesProperty.Changed.AddClassHandler<Control>((c, _) => Update(c));
        // New content while busy: the spinner moves to the new content's icon once it is laid out.
        ContentControl.ContentProperty.Changed.AddClassHandler<ContentControl>((c, _) =>
        {
            if (GetIsBusy(c)) Later(c);
        });
    }

    public static bool GetIsBusy(Control element) => element.GetValue(IsBusyProperty);
    public static void SetIsBusy(Control element, bool value) => element.SetValue(IsBusyProperty, value);

    public static bool GetDisables(Control element) => element.GetValue(DisablesProperty);
    public static void SetDisables(Control element, bool value) => element.SetValue(DisablesProperty, value);

    private static void Update(Control c)
    {
        var busy = GetIsBusy(c);
        var state = s_states.GetOrCreateValue(c);
        ((IPseudoClasses)c.Classes).Set(BusyPseudoClass, busy);

        // IsEnabled is held false at animation priority, above a local value or a binding, and the hold is dropped when busy ends.
        var off = busy && GetDisables(c);
        if (off && state.Off is null) state.Off = c.SetValue(InputElement.IsEnabledProperty, false, BindingPriority.Animation);
        else if (!off && state.Off is not null)
        {
            state.Off.Dispose();
            state.Off = null;
        }

        if (!busy) Release(state);
        else if (c.IsLoaded) Place(c, state);
        else Later(c);
    }

    // Places the spinner once the button is in a window and laid out, when its icon and template exist.
    private static void Later(Control c)
    {
        var state = s_states.GetOrCreateValue(c);
        if (state.Waiting) return;
        state.Waiting = true;
        if (c.IsLoaded)
            Dispatcher.UIThread.Post(() => Settle(c), DispatcherPriority.Loaded);
        else
            c.Loaded += OnLoaded;
    }

    private static void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var c = (Control)sender!;
        c.Loaded -= OnLoaded;
        Settle(c);
    }

    private static void Settle(Control c)
    {
        var state = s_states.GetOrCreateValue(c);
        state.Waiting = false;
        if (GetIsBusy(c)) Place(c, state);
    }

    private static void Place(Control c, State state)
    {
        var icon = Leading(c);
        if (icon is not null && icon == state.Icon) return;
        Release(state);
        if (icon is not null)
        {
            state.Icon = icon;
            icon.Wait(true);
            return;
        }
        // No icon: the face that draws the content shows the spinner in its place (a split button's main half).
        var face = c is SplitButton split
            ? split.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.TemplatedParent == split && b.Name == "PART_PrimaryButton")
            : c;
        if (face is null) return;
        state.Face = face;
        ((IPseudoClasses)face.Classes).Set(ContentPseudoClass, true);
    }

    private static void Release(State state)
    {
        state.Icon?.Wait(false);
        state.Icon = null;
        if (state.Face is not null) ((IPseudoClasses)state.Face.Classes).Set(ContentPseudoClass, false);
        state.Face = null;
    }

    // The first shown icon in the button's content, in reading order; a trailing one (class trail) stays.
    private static Icon? Leading(Control c) =>
        c.GetVisualDescendants().OfType<Icon>().FirstOrDefault(i => !i.Classes.Contains("trail") && i.IsEffectivelyVisible);
}
