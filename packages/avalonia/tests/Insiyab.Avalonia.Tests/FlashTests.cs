using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class FlashTests
{
    // Entrances run on a clock this holds and steps, so a slow run sees the same frames as a fast one.
    private sealed class Held : IDisposable
    {
        private readonly Func<double> _real = Entrance.Now;
        private double _at = 1000;

        public Held() => Entrance.Now = () => _at;

        public void To(double milliseconds, int settle = 60)
        {
            _at = 1000 + milliseconds;
            Settle(settle);
        }

        public void Dispose() => Entrance.Now = _real;
    }

    private static Color Tint(Visual v) => ColorOf((IBrush)Res(v, "InsToneBrandBg"));

    private static Color? Ground(Border b) => (b.Background as ISolidColorBrush)?.Color;

    [AvaloniaFact]
    public void A_flashing_row_lights_up_holds_and_fades_back_to_nothing()
    {
        using var clock = new Held();
        var row = new Border { Height = 40, Width = 300 };
        Entrance.SetEffect(row, EntranceEffect.Flash);
        Show(new StackPanel { Margin = new Thickness(20), Children = { row } });
        clock.To(0);
        clock.To(500);
        Assert.Equal(Tint(row), Ground(row));
        clock.To(1350);
        var half = Ground(row)!.Value;
        Assert.InRange(half.A, 1, Tint(row).A - 1);
        clock.To(2000);
        Assert.Null(row.Background);
    }

    [AvaloniaFact]
    public void A_flash_over_a_solid_ground_blends_and_gives_the_ground_back()
    {
        using var clock = new Held();
        var ground = new SolidColorBrush(Colors.White);
        var row = new Border { Height = 40, Width = 300, Background = ground };
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { row } });
        Entrance.Play(row, EntranceEffect.Flash);
        clock.To(0);
        clock.To(1350);
        var mid = Ground(row)!.Value;
        Assert.Equal(255, mid.A);
        Assert.NotEqual(Colors.White, mid);
        clock.To(2000);
        Assert.Same(ground, row.Background);
    }

    [AvaloniaFact]
    public void A_still_window_never_flashes()
    {
        using var clock = new Held();
        var row = new Border { Height = 40, Width = 300 };
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { row } });
        Motion.SetStill(w, true);
        Settle(60);
        Entrance.Play(row, EntranceEffect.Flash);
        clock.To(0);
        clock.To(500);
        Assert.Null(row.Background);
    }

    [AvaloniaFact]
    public void A_list_row_flashes_over_its_hover_and_keeps_its_own_look_after()
    {
        using var clock = new Held();
        var list = new ListBox { ItemsSource = new[] { "أ", "ب", "ج" }, Width = 300 };
        Show(list, FlowDirection.RightToLeft);
        var item = (ListBoxItem)list.ContainerFromIndex(1)!;
        var before = item.Background;
        Entrance.Play(item, EntranceEffect.Flash);
        clock.To(0);
        clock.To(500);
        Assert.Equal(Tint(item), ColorOf(item.Background));
        clock.To(2000);
        Assert.Equal(before, item.Background);
    }
}
