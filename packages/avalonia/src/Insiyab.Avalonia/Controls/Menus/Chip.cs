using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-chip</c>: a small value a person acts on. With <see cref="IsRemovable"/> it carries the × (<c>.ins-chip-x</c>), which raises the cancelable <see cref="RemovingEvent"/> and then takes the chip away. A filter chip is a <c>ToggleButton Classes="chip"</c>, a link chip a <c>Button Classes="chip"</c>.</summary>
[TemplatePart("PART_Remove", typeof(Button))]
[PseudoClasses(":removable")]
public class Chip : ContentControl
{
    public static readonly StyledProperty<bool> IsRemovableProperty =
        AvaloniaProperty.Register<Chip, bool>(nameof(IsRemovable));

    /// <summary>The word before the chip's text in the ×'s accessible name: «إزالة الرياض».</summary>
    public static readonly StyledProperty<string> RemoveLabelProperty =
        AvaloniaProperty.Register<Chip, string>(nameof(RemoveLabel), "إزالة");

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<Chip, object?>(nameof(Value));

    /// <summary>Raised before the chip goes; set <c>Cancel</c> to keep it (<c>ins:chip-remove</c>).</summary>
    public static readonly RoutedEvent<CancelRoutedEventArgs> RemovingEvent =
        RoutedEvent.Register<Chip, CancelRoutedEventArgs>(nameof(Removing), RoutingStrategies.Bubble);

    /// <summary>Raised once the chip is on its way out.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> RemovedEvent =
        RoutedEvent.Register<Chip, RoutedEventArgs>(nameof(Removed), RoutingStrategies.Bubble);

    private Button? _remove;

    static Chip()
    {
        IsRemovableProperty.Changed.AddClassHandler<Chip>((c, _) => c.PseudoClasses.Set(":removable", c.IsRemovable));
    }

    public bool IsRemovable { get => GetValue(IsRemovableProperty); set => SetValue(IsRemovableProperty, value); }
    public string RemoveLabel { get => GetValue(RemoveLabelProperty); set => SetValue(RemoveLabelProperty, value); }

    /// <summary>The value the chip stands for; its text when unset.</summary>
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public event EventHandler<CancelRoutedEventArgs>? Removing { add => AddHandler(RemovingEvent, value); remove => RemoveHandler(RemovingEvent, value); }
    public event EventHandler<RoutedEventArgs>? Removed { add => AddHandler(RemovedEvent, value); remove => RemoveHandler(RemovedEvent, value); }

    /// <summary>Whether the chip is leaving: removed, its exit still playing.</summary>
    public bool IsLeaving { get; private set; }

    internal Button? RemoveButton => _remove;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_remove is not null) _remove.Click -= OnRemoveClick;
        _remove = e.NameScope.Find<Button>("PART_Remove");
        if (_remove is not null)
        {
            _remove.Click += OnRemoveClick;
            UpdateRemoveName();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty || change.Property == RemoveLabelProperty) UpdateRemoveName();
    }

    private void UpdateRemoveName()
    {
        if (_remove is not null) AutomationProperties.SetName(_remove, $"{RemoveLabel} {Content}");
    }

    private void OnRemoveClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Remove();
    }

    /// <summary>Takes the chip away, as its × does; false when a <see cref="Removing"/> handler kept it.</summary>
    public bool Remove()
    {
        if (IsLeaving) return false;
        var ask = new CancelRoutedEventArgs(RemovingEvent, this);
        RaiseEvent(ask);
        if (ask.Cancel) return false;

        var hadFocus = IsKeyboardFocusWithin;
        var next = Neighbour();
        IsLeaving = true;
        IsHitTestVisible = false;
        RaiseEvent(new RoutedEventArgs(RemovedEvent, this));
        if (hadFocus) next?.Focus();
        _ = LeaveAsync();
        return true;
    }

    // The next chip's ×, else the previous one's, else the box of the field the chips are in.
    private Control? Neighbour()
    {
        if (Parent is not Panel panel) return null;
        var at = panel.Children.IndexOf(this);
        for (var i = at + 1; i < panel.Children.Count; i++)
            if (panel.Children[i] is Chip { IsLeaving: false, RemoveButton: { } b }) return b;
        for (var i = at - 1; i >= 0; i--)
            if (panel.Children[i] is Chip { IsLeaving: false, RemoveButton: { } b }) return b;
        for (var i = at + 1; i < panel.Children.Count; i++)
            if (panel.Children[i] is TextBox box) return box;
        return null;
    }

    private async Task LeaveAsync()
    {
        TransformOperationsAnimator.Ensure();
        if (TopLevel.GetTopLevel(this) is null)
        {
            if (Parent is Panel gone) gone.Children.Remove(this);
            return;
        }
        var leave = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new SplineEasing(0.4, 0, 1, 1),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, 1d), new Setter(RenderTransformProperty, TransformOperations.Identity) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, 0d), new Setter(RenderTransformProperty, TransformOperations.Parse("translateY(12px) scale(0.55) rotate(10deg)")) } },
            },
        };
        try { await leave.RunAsync(this, CancellationToken.None); }
        finally
        {
            if (Parent is Panel panel) panel.Children.Remove(this);
        }
    }

    /// <summary>The arrival a chip makes when it is added: it drops in, grows and straightens.</summary>
    public void PlayEnter()
    {
        TransformOperationsAnimator.Ensure();
        var enter = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(460),
            Easing = new SplineEasing(0.34, 1.56, 0.64, 1),
            FillMode = FillMode.Backward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, 0d), new Setter(RenderTransformProperty, TransformOperations.Parse("translateY(-10px) scale(0.4) rotate(-8deg)")) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, 1d), new Setter(RenderTransformProperty, TransformOperations.Identity) } },
            },
        };
        _ = enter.RunAsync(this, CancellationToken.None);
    }
}
