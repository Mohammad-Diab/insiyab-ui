using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>What became of the last change, the web's <c>data-ins-state</c>.</summary>
public enum SaveState
{
    /// <summary>Nothing to say: the indicator keeps its place and shows nothing.</summary>
    Idle,
    Saving,
    Saved,
    Error,
}

/// <summary>Whether the last change has been kept: saving, then saved, or could not save. The web's <c>.ins-save</c>; with <see cref="SaveState.Idle"/> it holds its place empty so the line does not jump.</summary>
[PseudoClasses(":saving", ":saved", ":error", ":idle")]
public class SaveStatus : TemplatedControl
{
    public static readonly StyledProperty<SaveState> StateProperty =
        AvaloniaProperty.Register<SaveStatus, SaveState>(nameof(State));

    public static readonly StyledProperty<string> SavingTextProperty =
        AvaloniaProperty.Register<SaveStatus, string>(nameof(SavingText), "جارٍ الحفظ…");

    public static readonly StyledProperty<string> SavedTextProperty =
        AvaloniaProperty.Register<SaveStatus, string>(nameof(SavedText), "حُفظ");

    public static readonly StyledProperty<string> ErrorTextProperty =
        AvaloniaProperty.Register<SaveStatus, string>(nameof(ErrorText), "تعذر الحفظ");

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<SaveStatus, string?>(nameof(Text));

    public static readonly DirectProperty<SaveStatus, string> DisplayTextProperty =
        AvaloniaProperty.RegisterDirect<SaveStatus, string>(nameof(DisplayText), o => o.DisplayText);

    private string _displayText = "";

    public SaveStatus() => Update();

    public SaveState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }

    public string SavingText { get => GetValue(SavingTextProperty); set => SetValue(SavingTextProperty, value); }

    public string SavedText { get => GetValue(SavedTextProperty); set => SetValue(SavedTextProperty, value); }

    public string ErrorText { get => GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }

    /// <summary>Words for the current state only, as <c>Insiyab.saveState(el, 'saved', 'حُفظت المسودة')</c> does; cleared by the next <see cref="SetState"/>.</summary>
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>What the indicator says now.</summary>
    public string DisplayText { get => _displayText; private set => SetAndRaise(DisplayTextProperty, ref _displayText, value); }

    /// <summary>Sets the state, with optional words for it.</summary>
    public void SetState(SaveState state, string? text = null)
    {
        SetCurrentValue(TextProperty, text);
        SetCurrentValue(StateProperty, state);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty || change.Property == TextProperty || change.Property == SavingTextProperty
            || change.Property == SavedTextProperty || change.Property == ErrorTextProperty)
            Update();
    }

    private void Update()
    {
        var state = State;
        PseudoClasses.Set(":idle", state == SaveState.Idle);
        PseudoClasses.Set(":saving", state == SaveState.Saving);
        PseudoClasses.Set(":saved", state == SaveState.Saved);
        PseudoClasses.Set(":error", state == SaveState.Error);
        DisplayText = state switch
        {
            SaveState.Saving => Text ?? SavingText,
            SaveState.Saved => Text ?? SavedText,
            SaveState.Error => Text ?? ErrorText,
            _ => "",
        };
    }
}

/// <summary>The arguments of <see cref="Autosave.SavingEvent"/>: set <c>Handled</c> to save somewhere yourself, then call <see cref="Done"/>.</summary>
public sealed class AutosaveEventArgs : RoutedEventArgs
{
    private readonly Action<bool> _done;

    internal AutosaveEventArgs(RoutedEvent routedEvent, object source, Action<bool> done) : base(routedEvent, source) => _done = done;

    /// <summary>Reports the save: true says saved, false says it could not be saved.</summary>
    public void Done(bool ok) => _done(ok);
}

/// <summary>A form that keeps itself, the web's <c>data-ins-autosave</c>: a moment after the last edit inside the container its <see cref="SaveStatus"/> says saving and <see cref="SavingEvent"/> goes out. Left unhandled, the change counts as kept at once.</summary>
public static class Autosave
{
    /// <summary>The wait after the last edit, in milliseconds; setting it turns autosave on (800 on the web).</summary>
    public static readonly AttachedProperty<int> DelayProperty =
        AvaloniaProperty.RegisterAttached<Control, int>("Delay", typeof(Autosave));

    /// <summary>A <see cref="SaveStatus"/> outside the container that reports for it, the web's <c>data-ins-save-for</c>.</summary>
    public static readonly AttachedProperty<SaveStatus?> IndicatorProperty =
        AvaloniaProperty.RegisterAttached<Control, SaveStatus?>("Indicator", typeof(Autosave));

    /// <summary>Raised on the container when a save is due, the web's <c>ins:autosave</c>.</summary>
    public static readonly RoutedEvent<AutosaveEventArgs> SavingEvent =
        RoutedEvent.Register<AutosaveEventArgs>("Saving", RoutingStrategies.Bubble, typeof(Autosave));

    private static readonly AttachedProperty<State?> StateProperty =
        AvaloniaProperty.RegisterAttached<Control, State?>("AutosaveState", typeof(Autosave));

    private sealed class State
    {
        public DispatcherTimer? Timer;
        public int Seq;
    }

    static Autosave()
    {
        DelayProperty.Changed.AddClassHandler<Control>((c, e) => Attach(c, e.GetNewValue<int>()));
    }

    public static int GetDelay(Control c) => c.GetValue(DelayProperty);

    public static void SetDelay(Control c, int value) => c.SetValue(DelayProperty, value);

    public static SaveStatus? GetIndicator(Control c) => c.GetValue(IndicatorProperty);

    public static void SetIndicator(Control c, SaveStatus? value) => c.SetValue(IndicatorProperty, value);

    public static void AddSavingHandler(Control c, EventHandler<AutosaveEventArgs> handler) => c.AddHandler(SavingEvent, handler);

    public static void RemoveSavingHandler(Control c, EventHandler<AutosaveEventArgs> handler) => c.RemoveHandler(SavingEvent, handler);

    private static void Attach(Control c, int delay)
    {
        c.RemoveHandler(TextBox.TextChangedEvent, OnEdit);
        c.RemoveHandler(ToggleButton.IsCheckedChangedEvent, OnEdit);
        c.RemoveHandler(SelectingItemsControl.SelectionChangedEvent, OnEdit);
        if (delay <= 0) return;
        c.AddHandler(TextBox.TextChangedEvent, OnEdit);
        c.AddHandler(ToggleButton.IsCheckedChangedEvent, OnEdit);
        c.AddHandler(SelectingItemsControl.SelectionChangedEvent, OnEdit);
    }

    private static void OnEdit(object? sender, RoutedEventArgs e)
    {
        if (sender is Control c) Schedule(c);
    }

    /// <summary>Starts the wait again, as an edit inside the container does.</summary>
    public static void Schedule(Control container)
    {
        var state = container.GetValue(StateProperty);
        if (state is null)
        {
            state = new State();
            container.SetValue(StateProperty, state);
        }
        state.Timer?.Stop();
        var wait = GetDelay(container) > 0 ? GetDelay(container) : 800;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(wait) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Save(container, state);
        };
        state.Timer = timer;
        timer.Start();
    }

    private static void Save(Control container, State state)
    {
        var indicator = GetIndicator(container) ?? container.GetVisualDescendants().OfType<SaveStatus>().FirstOrDefault();
        var seq = ++state.Seq;
        indicator?.SetState(SaveState.Saving);
        void Done(bool ok)
        {
            if (state.Seq != seq) return;
            indicator?.SetState(ok ? SaveState.Saved : SaveState.Error);
        }
        var args = new AutosaveEventArgs(SavingEvent, container, Done);
        container.RaiseEvent(args);
        if (!args.Handled) Done(true);
    }
}
