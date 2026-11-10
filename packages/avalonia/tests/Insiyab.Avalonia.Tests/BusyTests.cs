using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class BusyTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M12 3v12 M7 10l5 5 5-5 M5 21h14");

    // A command that counts its runs and answers CanExecute as told.
    private sealed class Counted : ICommand
    {
        private bool _can = true;
        public int Runs { get; private set; }
        public bool Can { get => _can; set { _can = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => _can;
        public void Execute(object? parameter) => Runs++;
    }

    private static StackPanel Label(string text, out Icon icon)
    {
        icon = new Icon { Data = Glyph, VerticalAlignment = VerticalAlignment.Center };
        return new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { icon, new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center } } };
    }

    private static LoadingSpinner? Spinner(Icon icon) => icon.GetVisualChildren().OfType<LoadingSpinner>().FirstOrDefault();

    [AvaloniaFact]
    public void A_button_is_not_busy_by_default_and_busy_disables_by_default()
    {
        var button = new Button { Content = "Save" };
        Assert.False(Busy.GetIsBusy(button));
        Assert.True(Busy.GetDisables(button));
        Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        Assert.True(button.IsEffectivelyEnabled);
        Assert.DoesNotContain(":busy", button.Classes);
        Assert.False(Part<LoadingSpinner>(button, "PART_Busy").IsVisible);
    }

    [AvaloniaTheory]
    [InlineData("primary")]
    [InlineData("secondary")]
    public void Busy_turns_the_leading_icon_into_the_spinner_in_the_buttons_ink_and_the_size_stays(string look)
    {
        var button = new Button { Classes = { look, "sm" }, Content = Label("Join", out var icon) };
        Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        var before = (button.Bounds, icon.Bounds);
        Busy.SetIsBusy(button, true);
        Settle();
        Assert.Contains(":busy", button.Classes);
        Assert.True(icon.IsWaiting);
        var spinner = Spinner(icon);
        Assert.NotNull(spinner);
        Assert.True(spinner.IsVisible);
        Assert.Equal(icon.Bounds.Size, spinner.Bounds.Size);
        Assert.Null(spinner.Arc);
        Assert.Equal(ColorOf(button.Foreground), ColorOf(spinner.Foreground));
        Assert.Equal(0, icon.GetValue(Icon.GlyphOpacityProperty), 2);
        Assert.Equal(before, (button.Bounds, icon.Bounds));
        // the disabled look, dimmed less so the spinner reads
        Assert.False(button.IsEffectivelyEnabled);
        Assert.Equal(0.75, button.Opacity, 2);

        Busy.SetIsBusy(button, false);
        Settle();
        Assert.False(icon.IsWaiting);
        Assert.False(spinner.IsVisible);
        Assert.Equal(1, icon.GetValue(Icon.GlyphOpacityProperty), 2);
        Assert.True(button.IsEffectivelyEnabled);
        Assert.Equal(1, button.Opacity, 2);
        Assert.Equal(before, (button.Bounds, icon.Bounds));
    }

    [AvaloniaFact]
    public void A_command_does_not_run_while_busy_and_runs_again_after()
    {
        var command = new Counted();
        var button = new Button { Content = Label("Join", out _), Command = command };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        Busy.SetIsBusy(button, true);
        Settle(100);
        Click(button, window);
        button.Focus();
        PressSpace(window);
        Assert.Equal(0, command.Runs);
        Busy.SetIsBusy(button, false);
        Settle(100);
        Click(button, window);
        Assert.Equal(1, command.Runs);
    }

    [AvaloniaFact]
    public void Busy_holds_IsEnabled_underneath_and_gives_back_what_it_became_meanwhile()
    {
        var button = new Button { Content = "Save" };
        Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        Busy.SetIsBusy(button, true);
        Assert.False(button.IsEffectivelyEnabled);
        // the app disables it meanwhile: it stays disabled after busy ends, and comes back when the app says so
        button.IsEnabled = false;
        Busy.SetIsBusy(button, false);
        Assert.False(button.IsEnabled);
        Assert.False(button.IsEffectivelyEnabled);
        button.IsEnabled = true;
        Assert.True(button.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void Busy_never_enables_a_button_whose_command_cannot_run()
    {
        var command = new Counted { Can = false };
        var button = new Button { Content = "Save", Command = command };
        Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        Assert.False(button.IsEffectivelyEnabled);
        Busy.SetIsBusy(button, true);
        command.Can = true;
        Assert.False(button.IsEffectivelyEnabled);
        Busy.SetIsBusy(button, false);
        Assert.True(button.IsEffectivelyEnabled);
        Busy.SetIsBusy(button, true);
        command.Can = false;
        Busy.SetIsBusy(button, false);
        Assert.False(button.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void Disables_false_keeps_a_busy_button_pressable()
    {
        var command = new Counted();
        var button = new Button { Content = Label("Stop", out var icon), Command = command };
        Busy.SetDisables(button, false);
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        Busy.SetIsBusy(button, true);
        Settle(100);
        Assert.True(icon.IsWaiting);
        Assert.True(button.IsEffectivelyEnabled);
        Click(button, window);
        Assert.Equal(1, command.Runs);
    }

    [AvaloniaFact]
    public void A_button_with_no_icon_turns_the_spinner_in_place_of_its_content()
    {
        var button = new Button { Classes = { "secondary" }, Content = "Save" };
        Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        var before = button.Bounds;
        Busy.SetIsBusy(button, true);
        Settle();
        Assert.Contains(":busy-content", button.Classes);
        Assert.Equal(0, Part<ContentPresenter>(button, "PART_ContentPresenter").Opacity, 2);
        var spinner = Part<LoadingSpinner>(button, "PART_Busy");
        Assert.True(spinner.IsVisible);
        Assert.Equal(ColorOf(button.Foreground), ColorOf(spinner.Foreground));
        Assert.Equal(before, button.Bounds);
        Busy.SetIsBusy(button, false);
        Settle();
        Assert.DoesNotContain(":busy-content", button.Classes);
        Assert.Equal(1, Part<ContentPresenter>(button, "PART_ContentPresenter").Opacity, 2);
        Assert.False(spinner.IsVisible);
    }

    [AvaloniaFact]
    public void An_icon_button_turns_its_icon_and_keeps_its_square()
    {
        var icon = new Icon { Data = Glyph };
        var button = new Button { Classes = { "bare", "icon" }, Content = icon };
        Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        var before = button.Bounds;
        Busy.SetIsBusy(button, true);
        Settle();
        Assert.True(icon.IsWaiting);
        Assert.Equal(icon.Bounds.Size, Spinner(icon)!.Bounds.Size);
        Assert.DoesNotContain(":busy-content", button.Classes);
        Assert.Equal(before, button.Bounds);
    }

    [AvaloniaFact]
    public void A_trailing_icon_stays_and_the_leading_one_turns()
    {
        var lead = new Icon { Data = Glyph };
        var trail = new Icon { Data = Glyph, Classes = { "trail" } };
        var button = new Button { Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { new TextBlock { Text = "Next" }, trail } } };
        var other = new Button { Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { lead, new TextBlock { Text = "Next" } } } };
        Show(new StackPanel { Margin = new Thickness(40), Spacing = 8, Children = { button, other } });
        Busy.SetIsBusy(button, true);
        Busy.SetIsBusy(other, true);
        Settle(100);
        Assert.False(trail.IsWaiting);
        Assert.Contains(":busy-content", button.Classes);
        Assert.True(lead.IsWaiting);
    }

    [AvaloniaFact]
    public void A_button_busy_before_it_is_shown_turns_once_it_is()
    {
        var button = new Button { Content = Label("Join", out var icon) };
        Busy.SetIsBusy(button, true);
        Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        Assert.True(icon.IsWaiting);
        Assert.False(button.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void A_split_button_turns_its_icon_and_neither_half_can_be_pressed()
    {
        var command = new Counted();
        var split = new SplitButton { Classes = { "primary", "sm" }, Content = Label("Join", out var icon), Command = command, Flyout = new MenuFlyout { Items = { new MenuItem { Header = "Copy" } } } };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { split } });
        var before = split.Bounds;
        Busy.SetIsBusy(split, true);
        Settle();
        Assert.True(icon.IsWaiting);
        Assert.False(split.IsEffectivelyEnabled);
        Assert.Equal(0.75, split.Opacity, 2);
        Click(Part<Button>(split, "PART_PrimaryButton"), window);
        Assert.Equal(0, command.Runs);
        Assert.Equal(before, split.Bounds);
        Busy.SetIsBusy(split, false);
        Settle();
        Assert.True(split.IsEffectivelyEnabled);
        Assert.False(icon.IsWaiting);
    }

    [AvaloniaFact]
    public void New_content_while_busy_moves_the_spinner_to_its_icon()
    {
        var button = new Button { Content = Label("Join", out var first) };
        Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        Busy.SetIsBusy(button, true);
        Settle(100);
        button.Content = Label("Joining", out var second);
        Settle(100);
        Assert.False(first.IsWaiting);
        Assert.True(second.IsWaiting);
    }

    [AvaloniaFact]
    public void A_still_window_swaps_the_icon_at_once()
    {
        var button = new Button { Content = Label("Join", out var icon) };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        Motion.SetStill(window, true);
        Settle(50);
        Busy.SetIsBusy(button, true);
        Assert.Equal(0, icon.GetValue(Icon.GlyphOpacityProperty), 2);
        Busy.SetIsBusy(button, false);
        Assert.Equal(1, icon.GetValue(Icon.GlyphOpacityProperty), 2);
    }
}
