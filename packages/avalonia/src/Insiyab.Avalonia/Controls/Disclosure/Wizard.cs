using System.Collections.Specialized;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>How a <see cref="Wizard"/> moves between steps.</summary>
public enum WizardMotion
{
    /// <summary>The new step fades in from 16px away while the height follows: for a form.</summary>
    Pane,

    /// <summary>The steps sit side by side and slide a step's width: for a sign-in card.</summary>
    Slide,
}

/// <summary>How a <see cref="Wizard"/> draws its steps.</summary>
public enum WizardStepsLook
{
    /// <summary>Numbered circles joined by lines, the web's <c>.ins-steps</c>.</summary>
    Numbers,

    /// <summary>One small dot per step, the web's <c>.ins-steps--dots</c>: for a sign-in card.</summary>
    Dots,
}

/// <summary>What a button inside a wizard does when pressed, the web's <c>data-ins-wizard</c>.</summary>
public enum WizardAction
{
    None,
    Next,
    Previous,
    Restart,
    Finish,
}

/// <summary>One step of a <see cref="Wizard"/>: <c>.ins-wizard-panel</c>. Its <c>Header</c> is the step's name in the steps above.</summary>
public class WizardPanel : HeaderedContentControl
{
    /// <summary>The web's <c>hidden</c> on a panel: the wizard skips it and drops its number from the steps.</summary>
    public static readonly StyledProperty<bool> IsSkippedProperty =
        AvaloniaProperty.Register<WizardPanel, bool>(nameof(IsSkipped));

    static WizardPanel()
    {
        FocusableProperty.OverrideDefaultValue<WizardPanel>(true);
        IsSkippedProperty.Changed.AddClassHandler<WizardPanel>((x, _) => (x.Parent as Wizard)?.Rebuild());
        HeaderProperty.Changed.AddClassHandler<WizardPanel>((x, _) => (x.Parent as Wizard)?.Rebuild());
    }

    public bool IsSkipped { get => GetValue(IsSkippedProperty); set => SetValue(IsSkippedProperty, value); }
}

/// <summary>Raised before "next" leaves a step. Call <see cref="Wait"/> to hold the wizard busy (a step that has to reach a server), then <see cref="Wizard.Proceed"/> or <see cref="Wizard.Stay"/>.</summary>
public class WizardLeavingEventArgs(RoutedEvent routedEvent, int step, int to, WizardPanel panel) : RoutedEventArgs(routedEvent)
{
    public int Step { get; } = step;

    public int To { get; } = to;

    public WizardPanel Panel { get; } = panel;

    public bool IsWaiting { get; private set; }

    public void Wait() => IsWaiting = true;
}

/// <summary>Raised when the finish button finds every step valid. Call <see cref="Wait"/> to hold the wizard busy while the answer is sent, then <see cref="Wizard.Stay"/> or <see cref="Wizard.Restart"/>.</summary>
public class WizardFinishedEventArgs(RoutedEvent routedEvent) : RoutedEventArgs(routedEvent)
{
    public bool IsWaiting { get; private set; }

    public void Wait() => IsWaiting = true;
}

/// <summary>Raised after every move: the step number among the steps showing, and its panel.</summary>
public class WizardMovedEventArgs(RoutedEvent routedEvent, int step, WizardPanel panel) : RoutedEventArgs(routedEvent)
{
    public int Step { get; } = step;

    public WizardPanel Panel { get; } = panel;
}

/// <summary>A form in steps, the web's <c>.ins-wizard</c>: the steps on top, one <see cref="WizardPanel"/> at a time, and back, next and finish below. "Next" (and Enter in a field) checks the step's fields first: their <see cref="Validate"/> rules, <see cref="RequiredProperty"/> ones that are empty, and any field already showing an error. Enter is next (finish on the last step), Alt and the arrow pointing back or forward and the mouse's back and forward buttons move between steps.</summary>
[TemplatePart("PART_Steps", typeof(Steps))]
[TemplatePart("PART_Body", typeof(Border))]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
[TemplatePart("PART_Previous", typeof(Button))]
[TemplatePart("PART_Next", typeof(Button))]
[TemplatePart("PART_Finish", typeof(Button))]
[TemplatePart("PART_Cancel", typeof(Button))]
[PseudoClasses(":first", ":last", ":busy", ":has-cancel")]
public class Wizard : ItemsControl
{
    public static readonly StyledProperty<int> CurrentIndexProperty =
        AvaloniaProperty.Register<Wizard, int>(nameof(CurrentIndex), defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<WizardMotion> MotionProperty =
        AvaloniaProperty.Register<Wizard, WizardMotion>(nameof(Motion));

    public static readonly StyledProperty<bool> AnimateHeightProperty =
        AvaloniaProperty.Register<Wizard, bool>(nameof(AnimateHeight), true);

    public static readonly StyledProperty<bool> ShowStepsProperty =
        AvaloniaProperty.Register<Wizard, bool>(nameof(ShowSteps), true);

    public static readonly StyledProperty<bool> ShowFootProperty =
        AvaloniaProperty.Register<Wizard, bool>(nameof(ShowFoot), true);

    public static readonly StyledProperty<WizardStepsLook> StepsLookProperty =
        AvaloniaProperty.Register<Wizard, WizardStepsLook>(nameof(StepsLook));

    public static readonly StyledProperty<string> PreviousTextProperty =
        AvaloniaProperty.Register<Wizard, string>(nameof(PreviousText), "السابق");

    public static readonly StyledProperty<string> NextTextProperty =
        AvaloniaProperty.Register<Wizard, string>(nameof(NextText), "التالي");

    public static readonly StyledProperty<string> FinishTextProperty =
        AvaloniaProperty.Register<Wizard, string>(nameof(FinishText), "إرسال");

    public static readonly DirectProperty<Wizard, bool> IsBusyProperty =
        AvaloniaProperty.RegisterDirect<Wizard, bool>(nameof(IsBusy), o => o.IsBusy);

    /// <summary>On a field in a step: "next" stops while it is empty (a TextBox), not whole (an OtpBox, with its own message) or unticked (a CheckBox), the web's <c>required</c>.</summary>
    public static readonly AttachedProperty<bool> RequiredProperty =
        AvaloniaProperty.RegisterAttached<Wizard, Control, bool>("Required");

    /// <summary>On a field in a step: it takes the focus when its step arrives, the web's <c>autofocus</c>.</summary>
    public static readonly AttachedProperty<bool> AutoFocusProperty =
        AvaloniaProperty.RegisterAttached<Wizard, Control, bool>("AutoFocus");

    /// <summary>On a button inside the wizard: what it does when pressed, the web's <c>data-ins-wizard</c>.</summary>
    public static readonly AttachedProperty<WizardAction> ActionProperty =
        AvaloniaProperty.RegisterAttached<Wizard, Button, WizardAction>("Action");

    public static readonly StyledProperty<object?> CancelContentProperty =
        AvaloniaProperty.Register<Wizard, object?>(nameof(CancelContent));

    public static readonly RoutedEvent<WizardLeavingEventArgs> LeavingEvent =
        RoutedEvent.Register<Wizard, WizardLeavingEventArgs>(nameof(Leaving), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<WizardMovedEventArgs> MovedEvent =
        RoutedEvent.Register<Wizard, WizardMovedEventArgs>(nameof(Moved), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<WizardFinishedEventArgs> FinishedEvent =
        RoutedEvent.Register<Wizard, WizardFinishedEventArgs>(nameof(Finished), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> CancelledEvent =
        RoutedEvent.Register<Wizard, RoutedEventArgs>(nameof(Cancelled), RoutingStrategies.Bubble);

    private static readonly Easing PaneEase = new SplineEasing(0.22, 1, 0.36, 1);
    private static readonly Easing SlideEase = new SplineEasing(0.4, 0, 0.2, 1);
    private readonly List<Control> _flagged = [];
    private Steps? _steps;
    private Border? _body;
    private ItemsPresenter? _presenter;
    private Button? _previous, _next, _finish, _cancel;
    private bool _busy;
    private bool _going;
    private bool _focusOnArrival;
    private CancellationTokenSource? _move;

    static Wizard()
    {
        TransformOperationsAnimator.Ensure();
        ActionProperty.Changed.AddClassHandler<Button>((b, e) =>
        {
            b.Click -= OnActionClick;
            if (e.GetNewValue<WizardAction>() != WizardAction.None) b.Click += OnActionClick;
        });
    }

    public Wizard()
    {
        ItemsView.CollectionChanged += (_, _) => Rebuild();
        AddHandler(KeyDownEvent, OnFieldKey, RoutingStrategies.Bubble);
        AddHandler(PointerPressedEvent, OnPointerButton, RoutingStrategies.Tunnel);
    }

    /// <summary>The step showing, counted among the panels that are not skipped.</summary>
    public int CurrentIndex { get => GetValue(CurrentIndexProperty); set => SetValue(CurrentIndexProperty, value); }

    public WizardMotion Motion { get => GetValue(MotionProperty); set => SetValue(MotionProperty, value); }

    /// <summary>The height follows the step smoothly (the web's <c>.ins-wizard-body</c>); off, it jumps.</summary>
    public bool AnimateHeight { get => GetValue(AnimateHeightProperty); set => SetValue(AnimateHeightProperty, value); }

    public bool ShowSteps { get => GetValue(ShowStepsProperty); set => SetValue(ShowStepsProperty, value); }

    /// <summary>The foot with back, next and finish, the web's optional <c>.ins-wizard-foot</c>; off, each step carries its own <see cref="ActionProperty"/> buttons.</summary>
    public bool ShowFoot { get => GetValue(ShowFootProperty); set => SetValue(ShowFootProperty, value); }

    /// <summary>The look of the steps on top: <see cref="WizardStepsLook.Dots"/> is the web's <c>.ins-steps--dots</c>.</summary>
    public WizardStepsLook StepsLook { get => GetValue(StepsLookProperty); set => SetValue(StepsLookProperty, value); }

    public string PreviousText { get => GetValue(PreviousTextProperty); set => SetValue(PreviousTextProperty, value); }

    public string NextText { get => GetValue(NextTextProperty); set => SetValue(NextTextProperty, value); }

    public string FinishText { get => GetValue(FinishTextProperty); set => SetValue(FinishTextProperty, value); }

    /// <summary>A bare cancel button at the start of the foot, back and next then together at the end: the web's wizard in a dialog. Null hides it.</summary>
    public object? CancelContent { get => GetValue(CancelContentProperty); set => SetValue(CancelContentProperty, value); }

    /// <summary>Waiting on a <see cref="Leaving"/> listener: the forward button spins and takes no second press.</summary>
    public bool IsBusy { get => _busy; private set { if (SetAndRaise(IsBusyProperty, ref _busy, value)) PseudoClasses.Set(":busy", value); } }

    public event EventHandler<WizardLeavingEventArgs>? Leaving { add => AddHandler(LeavingEvent, value); remove => RemoveHandler(LeavingEvent, value); }

    public event EventHandler<WizardMovedEventArgs>? Moved { add => AddHandler(MovedEvent, value); remove => RemoveHandler(MovedEvent, value); }

    /// <summary>The finish button was pressed and every step is valid: the web's form submit.</summary>
    public event EventHandler<WizardFinishedEventArgs>? Finished { add => AddHandler(FinishedEvent, value); remove => RemoveHandler(FinishedEvent, value); }

    /// <summary>The cancel button was pressed.</summary>
    public event EventHandler<RoutedEventArgs>? Cancelled { add => AddHandler(CancelledEvent, value); remove => RemoveHandler(CancelledEvent, value); }

    public static bool GetRequired(Control element) => element.GetValue(RequiredProperty);

    public static void SetRequired(Control element, bool value) => element.SetValue(RequiredProperty, value);

    public static bool GetAutoFocus(Control element) => element.GetValue(AutoFocusProperty);

    public static void SetAutoFocus(Control element, bool value) => element.SetValue(AutoFocusProperty, value);

    public static WizardAction GetAction(Button element) => element.GetValue(ActionProperty);

    public static void SetAction(Button element, WizardAction value) => element.SetValue(ActionProperty, value);

    /// <summary>The panels that are steps right now: every one not skipped.</summary>
    public IReadOnlyList<WizardPanel> Panels => All().Where(p => !p.IsSkipped).ToList();

    public void Next() => GoTo(CurrentIndex + 1, validate: true, focus: true);

    public void Previous() => GoTo(CurrentIndex - 1, validate: false, focus: true);

    public void Restart() => GoTo(0, validate: false, focus: true);

    /// <summary>After <see cref="WizardLeavingEventArgs.Wait"/>: the step was accepted, go on.</summary>
    public void Proceed() => GoTo(CurrentIndex + 1, validate: false, focus: true);

    /// <summary>After <see cref="WizardLeavingEventArgs.Wait"/> or <see cref="WizardFinishedEventArgs.Wait"/>: the step was refused, stay on it.</summary>
    public void Stay() => IsBusy = false;

    /// <summary>Goes to a step; with <paramref name="validate"/> a forward move checks the step being left and asks <see cref="Leaving"/> first. Returns the step showing afterwards.</summary>
    public int GoTo(int index, bool validate = false, bool focus = false)
    {
        var panels = Panels;
        if (panels.Count == 0) return 0;
        var cur = Math.Clamp(CurrentIndex, 0, panels.Count - 1);
        var to = Math.Clamp(index, 0, panels.Count - 1);
        if (validate && to > cur)
        {
            if (!IsValid(panels[cur])) return cur;
            var ask = new WizardLeavingEventArgs(LeavingEvent, cur, to, panels[cur]);
            RaiseEvent(ask);
            if (ask.IsWaiting)
            {
                IsBusy = true;
                return cur;
            }
        }
        IsBusy = false;
        _focusOnArrival = focus;
        _going = true;
        try
        {
            if (to == CurrentIndex) Apply(cur, to);
            else SetCurrentValue(CurrentIndexProperty, to);
        }
        finally
        {
            _going = false;
        }
        return to;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new WizardPanel();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<WizardPanel>(item, out recycleKey);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_previous is not null) _previous.Click -= OnPrevious;
        if (_next is not null) _next.Click -= OnNext;
        if (_finish is not null) _finish.Click -= OnFinish;
        if (_cancel is not null) _cancel.Click -= OnCancel;
        _steps = e.NameScope.Find<Steps>("PART_Steps");
        ApplyStepsLook();
        _body = e.NameScope.Find<Border>("PART_Body");
        _presenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
        _previous = e.NameScope.Find<Button>("PART_Previous");
        _next = e.NameScope.Find<Button>("PART_Next");
        _finish = e.NameScope.Find<Button>("PART_Finish");
        _cancel = e.NameScope.Find<Button>("PART_Cancel");
        if (_previous is not null) _previous.Click += OnPrevious;
        if (_next is not null) _next.Click += OnNext;
        if (_finish is not null) _finish.Click += OnFinish;
        if (_cancel is not null) _cancel.Click += OnCancel;
        Rebuild();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CurrentIndexProperty)
        {
            var from = change.GetOldValue<int>();
            var to = change.GetNewValue<int>();
            if (!_going) _focusOnArrival = false;
            Apply(from, to);
        }
        else if (change.Property == CancelContentProperty) PseudoClasses.Set(":has-cancel", change.NewValue is not null);
        else if (change.Property == StepsLookProperty) ApplyStepsLook();
    }

    private void ApplyStepsLook()
    {
        if (_steps is null) return;
        if (StepsLook == WizardStepsLook.Dots)
        {
            if (!_steps.Classes.Contains("dots")) _steps.Classes.Add("dots");
        }
        else _steps.Classes.Remove("dots");
    }

    private IEnumerable<WizardPanel> All() => Items.OfType<WizardPanel>();

    /// <summary>The steps from the panels' headers again, and every panel's state.</summary>
    internal void Rebuild()
    {
        if (_steps is not null)
        {
            var all = All().ToList();
            while (_steps.Items.Count > all.Count) _steps.Items.RemoveAt(_steps.Items.Count - 1);
            for (var i = 0; i < all.Count; i++)
            {
                if (i >= _steps.Items.Count) _steps.Items.Add(new StepsItem());
                if (_steps.Items[i] is StepsItem s) s.Content = all[i].Header;
            }
        }
        Apply(CurrentIndex, CurrentIndex, animate: false);
    }

    private void Apply(int fromIndex, int toIndex, bool animate = true)
    {
        var all = All().ToList();
        var panels = all.Where(p => !p.IsSkipped).ToList();
        if (panels.Count == 0) return;
        var to = Math.Clamp(toIndex, 0, panels.Count - 1);
        var from = Math.Clamp(fromIndex, 0, panels.Count - 1);
        var arrived = panels[to];
        var leaving = panels[from];
        var before = _body?.Bounds.Height ?? 0;
        _move?.Cancel();
        _move = null;
        foreach (var p in all)
        {
            p.IsVisible = p == arrived;
            p.ClearValue(OpacityProperty);
            p.ClearValue(RenderTransformProperty);
        }
        var at = all.IndexOf(arrived);
        if (_steps is not null)
            for (var j = 0; j < _steps.Items.Count && j < all.Count; j++)
                if (_steps.Items[j] is StepsItem s)
                {
                    s.IsVisible = !all[j].IsSkipped;
                    s.IsDone = j < at;
                    s.IsCurrent = j == at;
                }
        PseudoClasses.Set(":first", to == 0);
        PseudoClasses.Set(":last", to == panels.Count - 1);
        if (_body is not null) _body.Height = double.NaN;

        var moves = animate && leaving != arrived && _body is not null && TopLevel.GetTopLevel(this) is not null && before > 0;
        if (_body is not null) _body.ClipToBounds = moves;
        var focus = _focusOnArrival;
        _focusOnArrival = false;
        if (moves)
        {
            var cts = new CancellationTokenSource();
            _move = cts;
            _ = Motion == WizardMotion.Slide && AnimateHeight
                ? SlideAsync(leaving, arrived, to > from, before, focus, cts)
                : PaneAsync(arrived, to > from, before, cts);
        }
        if (focus && !(moves && Motion == WizardMotion.Slide && AnimateHeight)) FocusArrival(arrived);
        if (fromIndex != toIndex || moves) RaiseEvent(new WizardMovedEventArgs(MovedEvent, to, arrived));
    }

    private static TransformOperations ShiftX(double x) => TransformOperations.Parse(FormattableString.Invariant($"translateX({x}px)"));

    private double HeightOf(double width)
    {
        if (_body is null || _presenter is null) return 0;
        _presenter.Measure(new Size(Math.Max(0, width - _body.Padding.Left - _body.Padding.Right), double.PositiveInfinity));
        return _presenter.DesiredSize.Height + _body.Padding.Top + _body.Padding.Bottom;
    }

    private async Task PaneAsync(WizardPanel arrived, bool forward, double before, CancellationTokenSource cts)
    {
        var body = _body!;
        var after = HeightOf(body.Bounds.Width);
        var side = forward ? 1 : -1;
        var tasks = new List<Task>
        {
            Run(arrived, TimeSpan.FromMilliseconds(250), PaneEase, cts.Token,
                (0, [(OpacityProperty, 0d), (RenderTransformProperty, ShiftX(16d * side))]),
                (1, [(OpacityProperty, 1d), (RenderTransformProperty, TransformOperations.Identity)])),
        };
        if (AnimateHeight && Math.Abs(after - before) > 1)
        {
            body.Height = after;
            tasks.Add(Run(body, TimeSpan.FromMilliseconds(250), PaneEase, cts.Token,
                (0, [(HeightProperty, before)]), (1, [(HeightProperty, after)])));
        }
        await Task.WhenAll(tasks);
        if (cts.IsCancellationRequested) return;
        body.Height = double.NaN;
        body.ClipToBounds = false;
        if (_move == cts) _move = null;
    }

    private async Task SlideAsync(WizardPanel leaving, WizardPanel arrived, bool forward, double before, bool focus, CancellationTokenSource cts)
    {
        var body = _body!;
        var side = forward ? 1 : -1;
        var span = (arrived.Bounds.Width > 0 ? arrived.Bounds.Width : body.Bounds.Width) + 24;
        var after = HeightOf(body.Bounds.Width);
        leaving.IsVisible = true;
        arrived.Opacity = 0;
        arrived.RenderTransform = ShiftX(span * side);
        Task Resize()
        {
            body.Height = after;
            return Math.Abs(after - before) <= 1 ? Task.CompletedTask :
                Run(body, TimeSpan.FromMilliseconds(320), SlideEase, cts.Token, (0, [(HeightProperty, before)]), (1, [(HeightProperty, after)]));
        }
        Task Slide()
        {
            leaving.Opacity = 0;
            leaving.RenderTransform = ShiftX(-span * side);
            arrived.Opacity = 1;
            arrived.RenderTransform = TransformOperations.Identity;
            return Task.WhenAll(
                Run(leaving, TimeSpan.FromMilliseconds(320), SlideEase, cts.Token,
                    (0, [(RenderTransformProperty, TransformOperations.Identity), (OpacityProperty, 1d)]),
                    (0.7, [(OpacityProperty, 0d)]),
                    (1, [(RenderTransformProperty, ShiftX(-span * side)), (OpacityProperty, 0d)])),
                Run(arrived, TimeSpan.FromMilliseconds(320), SlideEase, cts.Token,
                    (0, [(RenderTransformProperty, ShiftX(span * side)), (OpacityProperty, 0d)]),
                    (0.7, [(OpacityProperty, 1d)]),
                    (1, [(RenderTransformProperty, TransformOperations.Identity), (OpacityProperty, 1d)])));
        }
        if (after > before)
        {
            await Resize();
            if (cts.IsCancellationRequested) return;
            await Slide();
        }
        else
        {
            body.Height = before;
            await Slide();
            if (cts.IsCancellationRequested) return;
            await Resize();
        }
        if (cts.IsCancellationRequested) return;
        leaving.IsVisible = false;
        leaving.ClearValue(RenderTransformProperty);
        leaving.ClearValue(OpacityProperty);
        arrived.ClearValue(RenderTransformProperty);
        arrived.ClearValue(OpacityProperty);
        body.Height = double.NaN;
        body.ClipToBounds = false;
        if (_move == cts) _move = null;
        if (focus && arrived.IsVisible) FocusArrival(arrived);
    }

    private static async Task Run(Animatable target, TimeSpan duration, Easing easing, CancellationToken token, params (double Cue, (AvaloniaProperty Property, object Value)[] Setters)[] frames)
    {
        var animation = new Animation { Duration = duration, Easing = easing };
        foreach (var (cue, setters) in frames)
        {
            var frame = new KeyFrame { Cue = new Cue(cue) };
            foreach (var (property, value) in setters) frame.Setters.Add(new Setter(property, value));
            animation.Children.Add(frame);
        }
        try { await animation.RunAsync(target, token); }
        catch (OperationCanceledException) { }
    }

    private static void FocusArrival(WizardPanel panel)
    {
        var field = panel.GetLogicalDescendants().OfType<Control>().FirstOrDefault(c => GetAutoFocus(c) && c.IsEffectivelyEnabled);
        if (field is not null) field.Focus(NavigationMethod.Tab);
        else panel.Focus();
    }

    /// <summary>Checks one step the way the browser checks a form: every field's <see cref="Validate"/> rules are reported, every field with an error keeps it, every empty <see cref="RequiredProperty"/> field gets one, and the first of them takes the focus.</summary>
    public bool IsValid(WizardPanel panel)
    {
        Control? first = null;
        foreach (var c in panel.GetLogicalDescendants().OfType<Control>())
        {
            if (!c.IsEffectivelyEnabled) continue;
            if (c is TextBox box) Validate.Report(box);
            var bad = DataValidationErrors.GetHasErrors(c);
            if (GetRequired(c) && Missing(c))
            {
                bad = true;
                DataValidationErrors.SetErrors(c, [MessageFor(c)]);
                Watch(c);
            }
            if (bad) first ??= c;
        }
        first?.Focus(NavigationMethod.Tab);
        return first is null;
    }

    private string RequiredMessage() => FlowDirection == FlowDirection.RightToLeft ? "هذا الحقل مطلوب." : "This field is required.";

    private string MessageFor(Control c) => c is OtpBox { ValidationMessage: { } partial } ? partial : RequiredMessage();

    private static bool Missing(Control c) => c switch
    {
        OtpBox o => !o.IsComplete,
        TextBox t => string.IsNullOrWhiteSpace(t.Text),
        ToggleButton b => b.IsChecked != true,
        _ => false,
    };

    private void Watch(Control c)
    {
        if (_flagged.Contains(c)) return;
        _flagged.Add(c);
        void Recheck()
        {
            if (Missing(c))
            {
                if (c is OtpBox) DataValidationErrors.SetErrors(c, [MessageFor(c)]);
                return;
            }
            DataValidationErrors.ClearErrors(c);
            _flagged.Remove(c);
            if (c is TextBox t) t.TextChanged -= OnText;
            if (c is ToggleButton b) b.IsCheckedChanged -= OnChecked;
        }
        void OnText(object? s, TextChangedEventArgs e) => Recheck();
        void OnChecked(object? s, RoutedEventArgs e) => Recheck();
        if (c is TextBox box) box.TextChanged += OnText;
        if (c is ToggleButton toggle) toggle.IsCheckedChanged += OnChecked;
    }

    private void OnPrevious(object? sender, RoutedEventArgs e) => Previous();

    private void OnNext(object? sender, RoutedEventArgs e)
    {
        if (!IsBusy) Next();
    }

    private void OnFinish(object? sender, RoutedEventArgs e) => Finish();

    private void OnCancel(object? sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(CancelledEvent));

    /// <summary>The finish button's press: checks every step, goes to the first one with a problem, or raises <see cref="Finished"/>.</summary>
    public void Finish()
    {
        if (IsBusy) return;
        var panels = Panels;
        for (var i = 0; i < panels.Count; i++)
        {
            if (IsValid(panels[i])) continue;
            if (i != CurrentIndex) GoTo(i);
            IsValid(panels[i]);
            return;
        }
        var done = new WizardFinishedEventArgs(FinishedEvent);
        RaiseEvent(done);
        if (done.IsWaiting) IsBusy = true;
    }

    private void OnFieldKey(object? sender, KeyEventArgs e)
    {
        if (e.Handled) return;
        if (e.KeyModifiers == KeyModifiers.Alt && e.Key is Key.Left or Key.Right)
        {
            e.Handled = true;
            var back = (e.Key == Key.Left) != (FlowDirection == FlowDirection.RightToLeft);
            if (back) Previous();
            else if (!IsBusy) Next();
            return;
        }
        if (e.Key != Key.Enter || e.KeyModifiers != KeyModifiers.None) return;
        if (e.Source is TextBox { AcceptsReturn: true }) return;
        if (Panels.Count == 0 || IsBusy) return;
        e.Handled = true;
        if (CurrentIndex >= Panels.Count - 1) Finish();
        else Next();
    }

    private void OnPointerButton(object? sender, PointerPressedEventArgs e)
    {
        var kind = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
        if (kind == PointerUpdateKind.XButton1Pressed)
        {
            e.Handled = true;
            Previous();
        }
        else if (kind == PointerUpdateKind.XButton2Pressed)
        {
            e.Handled = true;
            if (!IsBusy) Next();
        }
    }

    private static void OnActionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.FindLogicalAncestorOfType<Wizard>() is not { } w) return;
        switch (GetAction(b))
        {
            case WizardAction.Next: if (!w.IsBusy) w.Next(); break;
            case WizardAction.Previous: w.Previous(); break;
            case WizardAction.Restart: w.Restart(); break;
            case WizardAction.Finish: w.Finish(); break;
        }
    }
}
