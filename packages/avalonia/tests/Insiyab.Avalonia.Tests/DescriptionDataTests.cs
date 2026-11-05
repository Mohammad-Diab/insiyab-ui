using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A description list from data: one pair per item, reused by position; pairs that come later unfold in, pairs that go fold away.
public class DescriptionDataTests
{
    public sealed record FactRow(string Term, object Value);

    private static ObservableCollection<FactRow> Three() => [new("رقم الطلب", "1042"), new("العميل", "ليلى حداد"), new("المدينة", "الرياض")];

    private static DescriptionList Bound(ObservableCollection<FactRow> rows) => new()
    {
        ItemsSource = rows,
        TermBinding = new Binding(nameof(FactRow.Term)),
        ValueBinding = new Binding(nameof(FactRow.Value)),
    };

    private static List<DescriptionRow> Rows(DescriptionList dl) => dl.Children.OfType<DescriptionRow>().ToList();

    private static bool Has(Control c, string pseudo) => c.Classes.Contains(pseudo);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Take(ObservableCollection<FactRow> rows, DescriptionList dl, int index)
    {
        var row = Rows(dl)[index];
        rows.RemoveAt(index);
        return new WeakReference(row);
    }

    // Folds run on a clock this holds and steps, so a slow run sees the same frames as a fast one.
    private sealed class HeldClock : IDisposable
    {
        private readonly Func<double> _real = Fold.Now;
        private double _at;
        public HeldClock() { _at = _real(); Fold.Now = () => _at; }
        public void Step(Window w, double ms)
        {
            _at += ms;
            for (var i = 0; i < 3; i++) { w.InvalidateVisual(); Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
            Dispatcher.UIThread.RunJobs();
        }
        public void Dispose() => Fold.Now = _real;
    }

    private static bool Gone(WeakReference weak)
    {
        Settle(500);
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        return !weak.IsAlive;
    }

    [AvaloniaFact]
    public void Each_item_becomes_a_pair_with_its_term_and_value()
    {
        var pill = new Pill { Content = "مفتوح" };
        var rows = Three();
        rows.Add(new("الحالة", pill));
        var dl = Bound(rows);
        Show(dl, FlowDirection.RightToLeft);
        var pairs = Rows(dl).Select(r => r.Item).ToList();
        Assert.Equal(new[] { "رقم الطلب", "العميل", "المدينة", "الحالة" }, pairs.Select(p => (string?)p.Term).ToArray());
        Assert.Equal("ليلى حداد", pairs[1].Content);
        Assert.IsType<DescriptionText>(Part<ContentPresenter>(pairs[1], "PART_Value").Child);
        Assert.Same(pill, Part<ContentPresenter>(pairs[3], "PART_Value").Child);
        Assert.True(Has(pairs[0], ":first"));
        Assert.False(Has(pairs[1], ":first") || Has(pairs[1], ":last"));
        Assert.True(Has(pairs[3], ":last"));
    }

    [AvaloniaFact]
    public void The_first_fill_shows_at_once()
    {
        using var clock = new HeldClock();
        var dl = Bound(Three());
        Show(dl);
        Assert.All(Rows(dl), r =>
        {
            Assert.True(r.IsVisible);
            Assert.True(r.Bounds.Height > 10, $"height {r.Bounds.Height}");
            Assert.Equal(1, r.Opacity);
            Assert.Null(r.Clip);
        });
    }

    [AvaloniaFact]
    public void A_pair_added_later_unfolds_in()
    {
        var rows = Three();
        var dl = Bound(rows);
        var w = Show(dl);
        var before = dl.DesiredSize.Height;
        using var clock = new HeldClock();
        rows.Add(new("الشحن", "سريع"));
        clock.Step(w, 0);
        var row = Rows(dl)[3];
        Assert.True(row.IsVisible);
        Assert.InRange(row.Bounds.Height, 0, 1);
        clock.Step(w, 140);
        var mid = row.Bounds.Height;
        Assert.NotNull(row.Clip);
        clock.Step(w, 300);
        var full = row.Bounds.Height;
        Assert.True(full > 20, $"full {full}");
        Assert.InRange(mid, 1, full - 1);
        Assert.Null(row.Clip);
        Assert.Equal(1, row.Opacity);
        Assert.True(dl.DesiredSize.Height > before + full - 1);
    }

    [AvaloniaFact]
    public void A_removed_pair_folds_away_and_leaves()
    {
        var rows = Three();
        var dl = Bound(rows);
        var w = Show(dl);
        var last = Rows(dl)[2];
        var full = last.Bounds.Height;
        using var clock = new HeldClock();
        rows.RemoveAt(2);
        clock.Step(w, 0);
        Assert.Contains(last, dl.Children);
        Assert.True(last.IsLeaving);
        clock.Step(w, 90);
        Assert.InRange(last.Bounds.Height, 1, full - 1);
        clock.Step(w, 300);
        Assert.DoesNotContain(last, dl.Children);
        Assert.Equal(2, Rows(dl).Count);
        Assert.True(Has(Rows(dl)[1].Item, ":last"));
    }

    [AvaloniaFact]
    public void Pairs_are_reused_by_position()
    {
        var rows = Three();
        var dl = Bound(rows);
        Show(dl);
        var first = Rows(dl)[0];
        rows[0] = new("رقم الفاتورة", "2077");
        Settle(100);
        Assert.Same(first, Rows(dl)[0]);
        Assert.Equal("رقم الفاتورة", first.Item.Term);
        Assert.Equal("2077", first.Item.Content);
        Assert.Equal(3, Rows(dl).Count);
    }

    [AvaloniaFact]
    public void A_refill_while_a_pair_folds_away_makes_a_new_one()
    {
        var rows = Three();
        var dl = Bound(rows);
        var w = Show(dl);
        var leaving = Rows(dl)[2];
        using var clock = new HeldClock();
        rows.RemoveAt(2);
        clock.Step(w, 60);
        rows.Add(new("الشحن", "سريع"));
        clock.Step(w, 0);
        var fresh = Rows(dl).Last();
        Assert.NotSame(leaving, fresh);
        Assert.Equal("الشحن", fresh.Item.Term);
        clock.Step(w, 400);
        Assert.DoesNotContain(leaving, dl.Children);
        Assert.Equal(3, Rows(dl).Count);
    }

    [AvaloniaFact]
    public void Nothing_moves_in_a_still_window()
    {
        var rows = Three();
        var dl = Bound(rows);
        var w = Show(dl);
        Motion.SetStill(w, true);
        using var clock = new HeldClock();
        rows.Add(new("الشحن", "سريع"));
        clock.Step(w, 0);
        Assert.True(Rows(dl)[3].Bounds.Height > 10);
        rows.RemoveAt(0);
        clock.Step(w, 0);
        Assert.Equal(3, Rows(dl).Count);
    }

    [AvaloniaFact]
    public void A_list_out_of_sight_changes_at_once()
    {
        var rows = Three();
        var dl = Bound(rows);
        var host = new Border { Child = dl };
        var w = Show(host);
        host.IsVisible = false;
        Settle(50);
        using var clock = new HeldClock();
        rows.Add(new("الشحن", "سريع"));
        host.IsVisible = true;
        clock.Step(w, 0);
        Assert.True(Rows(dl)[3].Bounds.Height > 10);
    }

    [AvaloniaFact]
    public void Pairs_written_by_hand_stay_before_the_made_ones()
    {
        var hand = new DescriptionItem { Term = "المرجع", Content = "A-7" };
        var dl = new DescriptionList { Children = { hand } };
        Show(dl);
        dl.TermBinding = new Binding(nameof(FactRow.Term));
        dl.ValueBinding = new Binding(nameof(FactRow.Value));
        dl.ItemsSource = Three();
        Settle(100);
        Assert.Same(hand, dl.Children[0]);
        Assert.True(Has(hand, ":first"));
        Assert.Equal(3, Rows(dl).Count);
        dl.ItemsSource = null;
        Settle(600);
        Assert.Same(hand, Assert.Single(dl.Children));
        Assert.True(Has(hand, ":last"));
    }

    [AvaloniaFact]
    public void A_removed_pair_is_let_go()
    {
        var rows = Three();
        var dl = Bound(rows);
        Show(dl);
        Assert.True(Gone(Take(rows, dl, 2)));
        Assert.Equal(2, Rows(dl).Count);
    }
}
