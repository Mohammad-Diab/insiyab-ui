using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>The bar a table's selection brings up, the web's <c>.ins-bulkbar</c>: how many rows are selected, then the actions in <see cref="ContentControl.Content"/>. It shows only while a row of <see cref="Table"/> is selected.</summary>
[TemplatePart("PART_Count", typeof(TextBlock))]
public class BulkBar : ContentControl
{
    public static readonly StyledProperty<Table?> TableProperty =
        AvaloniaProperty.Register<BulkBar, Table?>(nameof(Table));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<BulkBar, string?>(nameof(Text));

    public static readonly DirectProperty<BulkBar, string> CountTextProperty =
        AvaloniaProperty.RegisterDirect<BulkBar, string>(nameof(CountText), o => o.CountText);

    /// <summary>On a button inside the bar: clears the selection, the web's <c>data-ins-select="none"</c>.</summary>
    public static readonly AttachedProperty<bool> ClearsSelectionProperty =
        AvaloniaProperty.RegisterAttached<BulkBar, Button, bool>("ClearsSelection");

    private string _countText = "";

    public BulkBar()
    {
        IsVisible = false;
        AddHandler(Button.ClickEvent, OnClick);
    }

    /// <summary>The table whose selection the bar follows: <c>data-ins-bulk</c>.</summary>
    public Table? Table { get => GetValue(TableProperty); set => SetValue(TableProperty, value); }

    /// <summary>How the count reads, with <c>{n}</c> for the number: <c>data-ins-text</c>. Unset, «المحدد: {n}» in RTL and «{n} selected» in LTR.</summary>
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>The count as the bar shows it.</summary>
    public string CountText { get => _countText; private set => SetAndRaise(CountTextProperty, ref _countText, value); }

    public static bool GetClearsSelection(Button button) => button.GetValue(ClearsSelectionProperty);

    public static void SetClearsSelection(Button button, bool value) => button.SetValue(ClearsSelectionProperty, value);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TableProperty)
        {
            if (change.OldValue is Table old) old.SelectionChanged -= OnSelection;
            if (change.NewValue is Table now) now.SelectionChanged += OnSelection;
            Update();
        }
        else if (change.Property == TextProperty || change.Property == FlowDirectionProperty)
        {
            Update();
        }
    }

    private void OnSelection(object? sender, TableSelectionEventArgs e) => Update();

    private void Update()
    {
        var count = Table?.SelectedRows.Count ?? 0;
        var format = Text ?? (FlowDirection == FlowDirection.RightToLeft ? "المحدد: {n}" : "{n} selected");
        CountText = format.Replace("{n}", count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        // It rises in each time a selection brings it up, as the web's does.
        if (count > 0 && !IsVisible && TopLevel.GetTopLevel(this) is not null) Entrance.Play(this, EntranceEffect.Rise);
        IsVisible = count > 0;
    }

    private void OnClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button button && GetClearsSelection(button)) Table?.SelectAll(false);
    }
}
