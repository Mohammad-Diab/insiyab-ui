using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// ins:PickerField: a secondary button holding a quiet label, the value, a dimmed detail and a magnifier or a chevron (a spinner while busy); it fills its column.
public class PickerFieldTests
{
    [AvaloniaFact]
    public void A_field_shows_its_label_value_detail_and_glyph_and_fills_its_column()
    {
        var field = new PickerField { Label = "البرنامج", Value = "الإجازة في تقانة المعلومات", Detail = "ITE", IsDetailCode = true };
        var clicked = 0;
        field.Click += (_, _) => clicked++;
        var w = Show(new Grid { Width = 420, Children = { field } }, FlowDirection.RightToLeft, width: 500, height: 200);
        Assert.Contains("secondary", field.Classes);
        Assert.Equal(420, field.Bounds.Width, 1);
        var texts = ((Grid)field.Content!).Children.OfType<TextBlock>().Where(t => t.IsVisible).Select(t => t.Text).ToList();
        Assert.Equal(["البرنامج", "الإجازة في تقانة المعلومات", "ITE"], texts);
        var code = field.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "ITE");
        Assert.Contains("num", code.Classes);
        var parts = ((Grid)field.Content!).Children;
        var glyph = parts.OfType<Icon>().Single();
        Assert.True(glyph.IsEffectivelyVisible);
        var search = glyph.Data;

        field.IsSearchable = false;
        Assert.NotSame(search, glyph.Data);
        field.Detail = null;
        Assert.DoesNotContain(field.GetVisualDescendants().OfType<TextBlock>(), t => t.IsEffectivelyVisible && t.Text == "ITE");

        field.IsBusy = true;
        Settle(50);
        Assert.False(glyph.IsEffectivelyVisible);
        Assert.True(parts.OfType<LoadingSpinner>().Single().IsEffectivelyVisible);

        field.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(1, clicked);
        Capture(w, "picker-field-rtl");
    }
}
