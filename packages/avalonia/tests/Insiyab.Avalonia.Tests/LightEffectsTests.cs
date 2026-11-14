using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class LightEffectsTests
{
    private static BoxShadows Shadow(TopLevel top, string key)
    {
        Assert.True(top.TryFindResource(key, top.ActualThemeVariant, out var value), key);
        return Assert.IsType<BoxShadows>(value);
    }

    private static List<BoxShadow> All(BoxShadows shadows)
    {
        var all = new List<BoxShadow>();
        foreach (var s in shadows) all.Add(s);
        return all;
    }

    private static List<BoxShadow> Drops(BoxShadows shadows) => All(shadows).Where(s => !s.IsInset && s.Blur > 0).ToList();

    [AvaloniaFact]
    public void Full_is_the_default_on_a_desktop()
    {
        InsiyabTheme.ResetEffects();
        Assert.Equal(EffectsLevel.Full, InsiyabTheme.Effects);
        var window = Show(new Border { Classes = { "card" } });
        Assert.DoesNotContain(InsiyabTheme.LightEffectsPseudoClass, window.Classes);
    }

    [AvaloniaFact]
    public void Light_keeps_one_smaller_drop_per_shadow_and_full_brings_them_back()
    {
        try
        {
            var card = new Border { Classes = { "card" }, Width = 200, Height = 100 };
            var window = Show(new StackPanel { Margin = new Thickness(40), Children = { card } }, theme: ThemeVariant.Dark);
            var md = Shadow(window, "InsShMd");
            var lg = Shadow(window, "InsShLg");
            Assert.Equal(2, Drops(md).Count);

            InsiyabTheme.Effects = EffectsLevel.Light;
            Assert.Contains(InsiyabTheme.LightEffectsPseudoClass, window.Classes);
            var light = Drops(Shadow(window, "InsShMd"));
            Assert.Single(light);
            Assert.Equal(All(md).Max(s => s.Blur), light[0].Blur);
            var big = Assert.Single(Drops(Shadow(window, "InsShLg")));
            Assert.Equal(12, big.Blur);
            Assert.Equal(All(lg)[0].Color, big.Color);
            Assert.Equal(All(lg)[0].OffsetY, big.OffsetY);

            InsiyabTheme.Effects = EffectsLevel.Full;
            Assert.DoesNotContain(InsiyabTheme.LightEffectsPseudoClass, window.Classes);
            Assert.Equal(md, Shadow(window, "InsShMd"));
            Assert.Equal(lg, Shadow(window, "InsShLg"));
        }
        finally { InsiyabTheme.ResetEffects(); }
    }

    [AvaloniaFact]
    public void Still_drops_the_light_drop_too()
    {
        try
        {
            InsiyabTheme.Effects = EffectsLevel.Light;
            var window = Show(new Border { Classes = { "card" } });
            Motion.SetStill(window, true);
            Assert.Empty(Drops(Shadow(window, "InsShLg")));
            Motion.SetStill(window, false);
            Assert.Single(Drops(Shadow(window, "InsShLg")));
        }
        finally { InsiyabTheme.ResetEffects(); }
    }

    [AvaloniaFact]
    public void Light_opens_a_dialog_without_blurring_the_page_and_asks_for_no_window_material()
    {
        try
        {
            var opener = new Button { Content = "فتح" };
            var window = Show(new StackPanel { Children = { opener } });
            var page = window.GetVisualDescendants().OfType<VisualLayerManager>().First().Child!;
            var dialog = new Dialog { Title = "حوار", Content = new TextBlock { Text = "نص" } };
            dialog.Open(opener);
            Settle();
            Assert.IsType<BlurEffect>(page.Effect);
            dialog.Close();
            Settle();
            Assert.Null(page.Effect);

            InsiyabTheme.Effects = EffectsLevel.Light;
            Assert.Equal([WindowTransparencyLevel.None], window.TransparencyLevelHint);
            dialog.Open(opener);
            Settle();
            Assert.Null(page.Effect);
            dialog.Close();
            Settle();
        }
        finally { InsiyabTheme.ResetEffects(); }
    }

    [AvaloniaFact]
    public void Light_hides_the_tab_glow_and_keeps_the_line()
    {
        try
        {
            var tabs = new TabControl { Items = { new TabItem { Header = "الأول", Content = "١" }, new TabItem { Header = "الثاني", Content = "٢" } }, SelectedIndex = 0 };
            Show(tabs);
            var first = (TabItem)tabs.Items[0]!;
            InsiyabTheme.Effects = EffectsLevel.Light;
            Settle(100);
            Assert.False(Part<Border>(first, "PART_PipeGlow").IsVisible);
            Assert.True(Part<Border>(first, "PART_Pipe").IsVisible);
            InsiyabTheme.Effects = EffectsLevel.Full;
            Settle(100);
            Assert.True(Part<Border>(first, "PART_PipeGlow").IsVisible);
        }
        finally { InsiyabTheme.ResetEffects(); }
    }

    [AvaloniaFact]
    public void A_low_memory_device_starts_light_and_the_app_can_override_it()
    {
        try
        {
            AndroidDevice.LowMemorySource = () => true;
            InsiyabTheme.ResetEffects();
            Assert.Equal(EffectsLevel.Light, InsiyabTheme.Effects);
            InsiyabTheme.Effects = EffectsLevel.Full;
            Assert.Equal(EffectsLevel.Full, InsiyabTheme.Effects);

            AndroidDevice.LowMemorySource = () => false;
            InsiyabTheme.ResetEffects();
            Assert.Equal(EffectsLevel.Full, InsiyabTheme.Effects);
            InsiyabTheme.Effects = EffectsLevel.Light;
            Assert.Equal(EffectsLevel.Light, InsiyabTheme.Effects);
        }
        finally
        {
            AndroidDevice.LowMemorySource = null;
            InsiyabTheme.ResetEffects();
        }
    }
}
