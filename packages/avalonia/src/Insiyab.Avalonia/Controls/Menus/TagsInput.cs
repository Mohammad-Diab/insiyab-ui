using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>One choice of a <see cref="TagsInput"/>'s list: <c>.ins-combo-option</c>.</summary>
public class TagsOption
{
    public TagsOption() { }

    public TagsOption(string value, string label)
    {
        Value = value;
        Label = label;
    }

    public string Value { get; set; } = string.Empty;

    /// <summary>What the option and its chip show; <see cref="Value"/> when empty.</summary>
    public string? Label { get; set; }

    internal string Text => string.IsNullOrEmpty(Label) ? Value : Label;
}

/// <summary>What changed in a <see cref="TagsInput"/>: every value now, and the one added or removed (<c>ins:tags</c>).</summary>
public class TagsChangedEventArgs : RoutedEventArgs
{
    internal TagsChangedEventArgs(RoutedEvent routedEvent, IReadOnlyList<string> values, string? added, string? removed) : base(routedEvent)
    {
        Values = values;
        Added = added;
        Removed = removed;
    }

    public IReadOnlyList<string> Values { get; }
    public string? Added { get; }
    public string? Removed { get; }
}

/// <summary>The web's <c>.ins-tags</c>: several values in one field, each a <see cref="Chip"/>, with the box after them. With <see cref="Options"/> it is a multi-select that matches as the autocomplete does and drops a chosen option from its list; without them, or with <see cref="IsFree"/>, what is typed becomes a tag on Enter or a comma. Backspace in the empty box takes the last one back.</summary>
[TemplatePart("PART_Field", typeof(Border))]
[TemplatePart("PART_Panel", typeof(Panel))]
[TemplatePart("PART_Input", typeof(TextBox))]
[TemplatePart("PART_Popup", typeof(Popup))]
[TemplatePart("PART_List", typeof(ItemsControl))]
[TemplatePart("PART_Empty", typeof(Control))]
[PseudoClasses(":open", ":focus-within-box")]
public class TagsInput : TemplatedControl
{
    public static readonly StyledProperty<bool> IsFreeProperty =
        AvaloniaProperty.Register<TagsInput, bool>(nameof(IsFree));

    public static readonly StyledProperty<int> MaxProperty =
        AvaloniaProperty.Register<TagsInput, int>(nameof(Max));

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<TagsInput, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<string> EmptyTextProperty =
        AvaloniaProperty.Register<TagsInput, string>(nameof(EmptyText), "لا نتائج");

    public static readonly StyledProperty<string> RemoveLabelProperty =
        AvaloniaProperty.Register<TagsInput, string>(nameof(RemoveLabel), "إزالة");

    public static readonly DirectProperty<TagsInput, bool> IsDropDownOpenProperty =
        AvaloniaProperty.RegisterDirect<TagsInput, bool>(nameof(IsDropDownOpen), o => o.IsDropDownOpen);

    public static readonly RoutedEvent<TagsChangedEventArgs> TagsChangedEvent =
        RoutedEvent.Register<TagsInput, TagsChangedEventArgs>(nameof(TagsChanged), RoutingStrategies.Bubble);

    private Panel? _panel;
    private TextBox? _input;
    private Popup? _popup;
    private ItemsControl? _list;
    private Control? _empty;
    private Border? _field;
    private bool _open;
    private int _active = -1;
    private bool _syncing;
    private readonly Dictionary<TagsOption, Border> _rows = [];
    private List<TagsOption> _visible = [];

    public TagsInput()
    {
        Values.CollectionChanged += OnValuesChanged;
        Options.CollectionChanged += (_, _) => BuildRows();
    }

    /// <summary>The values, in order; each is a chip.</summary>
    public AvaloniaList<string> Values { get; } = [];

    /// <summary>The list to choose from; none makes it a free tags field.</summary>
    [Content]
    public AvaloniaList<TagsOption> Options { get; } = [];

    /// <summary>Takes whatever is typed even with a list (<c>data-ins-free</c>).</summary>
    public bool IsFree { get => GetValue(IsFreeProperty); set => SetValue(IsFreeProperty, value); }

    /// <summary>The most values it takes; 0 for no limit (<c>data-ins-max</c>).</summary>
    public int Max { get => GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    public string? PlaceholderText { get => GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }

    /// <summary>The line the list shows when nothing matches: <c>.ins-combo-empty</c>.</summary>
    public string EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>The word before a chip's text in its ×'s accessible name.</summary>
    public string RemoveLabel { get => GetValue(RemoveLabelProperty); set => SetValue(RemoveLabelProperty, value); }

    public bool IsDropDownOpen { get => _open; private set => SetAndRaise(IsDropDownOpenProperty, ref _open, value); }

    public event EventHandler<TagsChangedEventArgs>? TagsChanged { add => AddHandler(TagsChangedEvent, value); remove => RemoveHandler(TagsChangedEvent, value); }

    /// <summary>The text box after the chips.</summary>
    internal TextBox? InputBox => _input;

    private bool Free => IsFree || Options.Count == 0;

    /// <summary>Adds a value as a chip, unless it is empty, already there, or past <see cref="Max"/>.</summary>
    public bool Add(string value)
    {
        value = value.Trim();
        if (value.Length == 0 || Values.Contains(value)) return false;
        if (Max > 0 && Values.Count >= Max) return false;
        Values.Add(value);
        return true;
    }

    /// <summary>Removes a value and its chip.</summary>
    public bool Remove(string value) => Values.Remove(value);

    private string LabelOf(string value) =>
        Options.FirstOrDefault(o => o.Value == value)?.Text ?? value;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_input is not null)
        {
            _input.RemoveHandler(KeyDownEvent, OnInputKeyDown);
            _input.RemoveHandler(TextInputEvent, OnInputText);
            _input.TextChanged -= OnInputTextChanged;
            _input.GotFocus -= OnInputGotFocus;
            _input.LostFocus -= OnInputLostFocus;
        }
        if (_list is not null) _list.ContainerClearing -= OnContainerClearing;
        _field = e.NameScope.Find<Border>("PART_Field");
        _panel = e.NameScope.Find<Panel>("PART_Panel");
        _input = e.NameScope.Find<TextBox>("PART_Input");
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _list = e.NameScope.Find<ItemsControl>("PART_List");
        _empty = e.NameScope.Find<Control>("PART_Empty");

        if (_input is not null)
        {
            _input.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
            _input.AddHandler(TextInputEvent, OnInputText, RoutingStrategies.Tunnel);
            _input.TextChanged += OnInputTextChanged;
            _input.GotFocus += OnInputGotFocus;
            _input.LostFocus += OnInputLostFocus;
        }
        if (_panel is not null)
        {
            var at = _input is null ? -1 : _panel.Children.IndexOf(_input);
            foreach (var v in Values)
            {
                if (at < 0) _panel.Children.Add(MakeChip(v));
                else _panel.Children.Insert(at++, MakeChip(v));
            }
        }
        if (_list is not null)
        {
            _list.ItemTemplate = new FuncDataTemplate<TagsOption>((option, _) => option is null ? null : BuildRow(option), supportsRecycling: false);
            _list.ContainerClearing += OnContainerClearing;
        }
        BuildRows();
        UpdatePlaceholder();
    }

    private void OnContainerClearing(object? sender, ContainerClearingEventArgs e)
    {
        if (e.Container is ContentPresenter { Child: Border { Tag: TagsOption option } row } && _rows.TryGetValue(option, out var kept) && kept == row) _rows.Remove(option);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Source is Visual v && v.FindAncestorOfType<Button>(true) is null && _input is not null && !_input.IsFocused)
        {
            _input.Focus();
            e.Handled = true;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PlaceholderTextProperty) UpdatePlaceholder();
        if (change.Property == RemoveLabelProperty && _panel is not null)
            foreach (var chip in _panel.Children.OfType<Chip>()) chip.RemoveLabel = RemoveLabel;
    }

    private Chip MakeChip(string value)
    {
        var chip = new Chip { Content = LabelOf(value), Value = value, IsRemovable = true, RemoveLabel = RemoveLabel, Focusable = false };
        chip.Removed += OnChipRemoved;
        return chip;
    }

    private void OnChipRemoved(object? sender, RoutedEventArgs e)
    {
        if (sender is not Chip chip || chip.Value is not string value) return;
        _syncing = true;
        try { Values.Remove(value); }
        finally { _syncing = false; }
        Changed(null, value);
    }

    private void OnValuesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdatePlaceholder();
        Refilter();
        if (_syncing || _panel is null) return;
        if (e.OldItems is not null)
            foreach (string v in e.OldItems)
            {
                var chip = _panel.Children.OfType<Chip>().FirstOrDefault(c => !c.IsLeaving && Equals(c.Value, v));
                if (chip is not null)
                {
                    chip.Removed -= OnChipRemoved;
                    if (!chip.Remove()) chip.Removed += OnChipRemoved;
                }
                Changed(null, v);
            }
        if (e.NewItems is not null)
            foreach (string v in e.NewItems)
            {
                var at = _input is not null ? _panel.Children.IndexOf(_input) : -1;
                Chip.Arrive(_panel, at < 0 ? _panel.Children.Count : at, MakeChip(v));
                Changed(v, null);
            }
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var chip in _panel.Children.OfType<Chip>().ToList()) _panel.Children.Remove(chip);
            foreach (var v in Values) _panel.Children.Insert(_panel.Children.Count - (_input is null ? 0 : 1), MakeChip(v));
        }
    }

    private void Changed(string? added, string? removed) =>
        RaiseEvent(new TagsChangedEventArgs(TagsChangedEvent, Values.ToList(), added, removed));

    private void UpdatePlaceholder()
    {
        if (_input is not null) _input.PlaceholderText = Values.Count > 0 ? null : PlaceholderText;
    }

    private void BuildRows() => Refilter();

    private Border BuildRow(TagsOption option)
    {
        var row = new Border { Classes = { "tags-option" }, Child = new TextBlock { Text = option.Text }, Focusable = false, Tag = option };
        if (_active >= 0 && _active < _visible.Count && _visible[_active] == option) row.Classes.Add("active");
        row.PointerEntered += (_, _) => Activate(_visible.IndexOf(option));
        row.PointerPressed += (_, e) => { e.Handled = true; };
        row.PointerReleased += (_, e) => { e.Handled = true; Choose(option); };
        _rows[option] = row;
        return row;
    }

    private void Refilter()
    {
        if (_list is null) return;
        var q = _input?.Text;
        _rows.Clear();
        _visible = Options.Where(o => !Values.Contains(o.Value) && ArabicSearch.Matches(q, o.Text, o.Value)).ToList();
        _list.ItemsSource = _visible;
        if (_empty is not null) _empty.IsVisible = _visible.Count == 0;
        Activate(_active >= _visible.Count ? -1 : _active);
    }

    private void Activate(int index, bool scroll = false)
    {
        foreach (var row in _rows.Values) row.Classes.Remove("active");
        _active = index >= 0 && index < _visible.Count ? index : -1;
        if (_active < 0) return;
        if (_rows.TryGetValue(_visible[_active], out var current)) current.Classes.Add("active");
        if (!scroll || _list is null) return;
        _list.ScrollIntoView(_active);
        if (_rows.TryGetValue(_visible[_active], out current)) current.Classes.Add("active");
    }

    private void Choose(TagsOption option)
    {
        Add(option.Value);
        if (_input is not null)
        {
            _input.Text = string.Empty;
            _input.Focus();
        }
        _active = -1;
        Refilter();
        SetOpen(true);
    }

    private void SetOpen(bool open)
    {
        if (Options.Count == 0) open = false;
        if (open == IsDropDownOpen) return;
        IsDropDownOpen = open;
        PseudoClasses.Set(":open", open);
        if (_popup is not null)
        {
            if (open && _field is not null)
            {
                _popup.PlacementTarget = _field;
                if (_popup.Child is Border { Child: Control c } frame)
                {
                    c.Width = _field.Bounds.Width;
                    var room = frame.Padding;
                    _popup.HorizontalOffset = FlowDirection == global::Avalonia.Media.FlowDirection.RightToLeft ? room.Right : -room.Left;
                    _popup.VerticalOffset = 6 - room.Top;
                }
            }
            _popup.IsOpen = open;
        }
        if (!open) Activate(-1);
    }

    private void OnInputGotFocus(object? sender, FocusChangedEventArgs e)
    {
        PseudoClasses.Set(":focus-within-box", true);
        Refilter();
        SetOpen(true);
    }

    private void OnInputLostFocus(object? sender, FocusChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_input is { IsFocused: true }) return;
            PseudoClasses.Set(":focus-within-box", false);
            SetOpen(false);
        });
    }

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        _active = -1;
        Refilter();
        if (_input?.IsFocused == true) SetOpen(true);
    }

    private void OnInputText(object? sender, TextInputEventArgs e)
    {
        if (e.Text is not ("," or "،") || _input is null) return;
        var text = _input.Text ?? string.Empty;
        if (!Free || text.Trim().Length == 0) return;
        e.Handled = true;
        Add(text);
        _input.Text = string.Empty;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (_input is null) return;
        switch (e.Key)
        {
            case Key.Down:
            case Key.Up:
                if (Options.Count == 0) return;
                e.Handled = true;
                if (!IsDropDownOpen) SetOpen(true);
                if (_visible.Count == 0) return;
                Activate(e.Key == Key.Down ? (_active + 1) % _visible.Count : (_active <= 0 ? _visible.Count - 1 : _active - 1), true);
                return;
            case Key.Enter:
                if (IsDropDownOpen && _active >= 0 && _active < _visible.Count)
                {
                    e.Handled = true;
                    Choose(_visible[_active]);
                    return;
                }
                var text = _input.Text ?? string.Empty;
                if (Free && text.Trim().Length > 0)
                {
                    e.Handled = true;
                    Add(text);
                    _input.Text = string.Empty;
                }
                return;
            case Key.Escape:
                if (!IsDropDownOpen) return;
                e.Handled = true;
                SetOpen(false);
                return;
            case Key.Back:
                if (!string.IsNullOrEmpty(_input.Text) || _input.CaretIndex != 0 || _panel is null) return;
                var last = _panel.Children.OfType<Chip>().LastOrDefault(c => !c.IsLeaving);
                if (last is null) return;
                e.Handled = true;
                last.Remove();
                return;
        }
    }
}
