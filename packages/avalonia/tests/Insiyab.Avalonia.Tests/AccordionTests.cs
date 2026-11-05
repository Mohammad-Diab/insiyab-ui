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

// ins:Accordion: sections from data with a head and a body, two-way open state, one open at a time, and dividers and corners that skip hidden sections.
public class AccordionTests
{
    public sealed class TopicRow(string question, string answer) : INotifyPropertyChanged
    {
        private bool _isOpen;
        public string Question { get; } = question;
        public string Answer { get; } = answer;
        public bool IsOpen { get => _isOpen; set { if (_isOpen == value) return; _isOpen = value; PropertyChanged?.Invoke(this, new(nameof(IsOpen))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private static ObservableCollection<TopicRow> Topics() =>
        [new("السؤال الأول", "الجواب الأول.") { IsOpen = true }, new("السؤال الثاني", "الجواب الثاني."), new("السؤال الثالث", "الجواب الثالث.")];

    private static Accordion Bound(ObservableCollection<TopicRow> topics, bool single = false) => new()
    {
        ItemsSource = topics,
        HeaderBinding = new Binding(nameof(TopicRow.Question)),
        IsExpandedBinding = new Binding(nameof(TopicRow.IsOpen)),
        ItemTemplate = new FuncDataTemplate<TopicRow>((t, _) => new TextBlock { Text = t?.Answer }),
        IsSingle = single,
    };

    private static Border InPanel(Control c) => new() { Classes = { "panel" }, Child = c };

    private static List<Expander> Sections(Accordion a) => a.GetRealizedContainers().OfType<Expander>().OrderBy(a.IndexFromContainer).ToList();

    private static ToggleButton HeaderOf(Expander e) => Part<ToggleButton>(e, "ExpanderHeader");

    private static void Press(Window w, Key key)
    {
        w.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        w.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(200);
    }

    private static Control? Focused(Window w) => w.FocusManager?.GetFocusedElement() as Control;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Take(ObservableCollection<TopicRow> topics, Accordion acc, int index)
    {
        var section = Sections(acc)[index];
        topics.RemoveAt(index);
        return new WeakReference(section);
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
    public void Items_become_sections_with_a_head_and_a_body()
    {
        var acc = Bound(Topics());
        Show(InPanel(acc), FlowDirection.RightToLeft);
        var s = Sections(acc);
        Assert.Equal(3, s.Count);
        Assert.Equal("السؤال الأول", s[0].Header);
        Assert.True(s[0].IsExpanded);
        Assert.False(s[1].IsExpanded);
        Assert.Contains(s[0].GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "الجواب الأول.");
    }

    [AvaloniaFact]
    public void Sections_take_the_dividers_and_the_panel_corners()
    {
        var topics = Topics();
        var acc = Bound(topics);
        Show(InPanel(acc));
        var s = Sections(acc);
        Assert.Equal(new CornerRadius(21, 21, 0, 0), HeaderOf(s[0]).CornerRadius);
        Assert.Equal(new CornerRadius(0), HeaderOf(s[1]).CornerRadius);
        Assert.Equal(new CornerRadius(0, 0, 21, 21), HeaderOf(s[2]).CornerRadius);
        Assert.Equal(new Thickness(0), s[0].BorderThickness);
        Assert.Equal(new Thickness(0, 1, 0, 0), s[1].BorderThickness);
        Assert.Equal(new Thickness(0, 1, 0, 0), s[2].BorderThickness);
        topics[2].IsOpen = true;
        Settle();
        Assert.Equal(new CornerRadius(0), HeaderOf(s[2]).CornerRadius);
    }

    [AvaloniaFact]
    public void Hidden_sections_are_skipped_by_the_dividers_and_corners()
    {
        var acc = Bound(Topics());
        Show(InPanel(acc));
        var s = Sections(acc);
        s[0].IsVisible = false;
        Settle(100);
        Assert.Equal(new CornerRadius(21, 21, 0, 0), HeaderOf(s[1]).CornerRadius);
        Assert.Equal(new Thickness(0), s[1].BorderThickness);
        s[2].IsVisible = false;
        Settle(100);
        Assert.Equal(new CornerRadius(21), HeaderOf(s[1]).CornerRadius);
    }

    [AvaloniaFact]
    public void Outside_a_panel_sections_keep_small_corners()
    {
        var acc = Bound(Topics());
        Show(acc);
        var s = Sections(acc);
        Assert.Equal((CornerRadius)Res(acc, "InsRSm"), HeaderOf(s[0]).CornerRadius);
        Assert.Equal(new Thickness(0, 1, 0, 0), s[1].BorderThickness);
    }

    [AvaloniaFact]
    public void Open_state_binds_both_ways()
    {
        var topics = Topics();
        var acc = Bound(topics);
        var w = Show(InPanel(acc));
        var s = Sections(acc);
        Click(HeaderOf(s[1]), w);
        Assert.True(topics[1].IsOpen);
        topics[1].IsOpen = false;
        Settle(100);
        Assert.False(s[1].IsExpanded);
        Click(HeaderOf(s[1]), w);
        Assert.True(topics[1].IsOpen);
        Assert.True(s[1].IsExpanded);
    }

    [AvaloniaFact]
    public void Single_keeps_one_section_open()
    {
        var topics = Topics();
        var acc = Bound(topics, single: true);
        Show(InPanel(acc));
        topics[2].IsOpen = true;
        Settle(100);
        var s = Sections(acc);
        Assert.False(topics[0].IsOpen);
        Assert.False(s[0].IsExpanded);
        Assert.True(s[2].IsExpanded);
    }

    [AvaloniaFact]
    public void Heads_are_a_keyboard_list_that_skips_hidden_sections()
    {
        var before = new Button { Content = "قبل" };
        var acc = Bound(Topics());
        var w = Show(new StackPanel { Children = { before, InPanel(acc) } });
        var s = Sections(acc);
        s[1].IsVisible = false;
        Settle(100);
        before.Focus(NavigationMethod.Tab);
        Press(w, Key.Tab);
        Assert.Same(HeaderOf(s[0]), Focused(w));
        Press(w, Key.Down);
        Assert.Same(HeaderOf(s[2]), Focused(w));
        Press(w, Key.Down);
        Assert.Same(HeaderOf(s[2]), Focused(w));
        Press(w, Key.Up);
        Assert.Same(HeaderOf(s[0]), Focused(w));
        Press(w, Key.End);
        Assert.Same(HeaderOf(s[2]), Focused(w));
        Press(w, Key.Home);
        Assert.Same(HeaderOf(s[0]), Focused(w));
    }

    [AvaloniaFact]
    public void Sections_written_by_hand_take_the_same_look()
    {
        var a = new Expander { Header = "أ", Content = "١" };
        var b = new Expander { Header = "ب", Content = "٢" };
        var acc = new Accordion { Items = { a, b } };
        Show(InPanel(acc));
        Assert.Equal(new CornerRadius(21, 21, 0, 0), HeaderOf(a).CornerRadius);
        Assert.Equal(new Thickness(0, 1, 0, 0), b.BorderThickness);
    }

    [AvaloniaFact]
    public void A_removed_section_is_let_go()
    {
        var topics = Topics();
        var acc = Bound(topics);
        Show(InPanel(acc));
        Assert.True(Gone(Take(topics, acc, 1)));
        var s = Sections(acc);
        Assert.Equal(2, s.Count);
        Assert.Equal(new Thickness(0, 1, 0, 0), s[1].BorderThickness);
    }
}
