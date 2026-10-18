using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a <see cref="ThemeToggle"/> asks the app theme to become.</summary>
public enum ThemeMode
{
    /// <summary>Flips light and dark.</summary>
    Toggle,
    Light,
    Dark,
    /// <summary>Follows the system.</summary>
    System,
}

/// <summary>The web's <c>data-ins-theme-toggle</c> and <c>Insiyab.theme()</c>: the app-wide theme, light, dark or following the system.</summary>
public static class ThemeToggle
{
    /// <summary>On a Button: a click sets the app theme through a <see cref="ThemeSweep"/> from the button. Unset or <see cref="ThemeMode.Toggle"/> flips light and dark; <see cref="ThemeMode.Dark"/>, <see cref="ThemeMode.Light"/> or <see cref="ThemeMode.System"/> sets that one.</summary>
    public static readonly AttachedProperty<ThemeMode?> ModeProperty =
        AvaloniaProperty.RegisterAttached<Button, ThemeMode?>("Mode", typeof(ThemeToggle));

    static ThemeToggle()
    {
        ModeProperty.Changed.AddClassHandler<Button>((button, e) =>
        {
            button.Click -= OnClick;
            if (e.GetNewValue<ThemeMode?>() is not null && button is not DayNight) button.Click += OnClick;
        });
    }

    public static ThemeMode? GetMode(Button button) => button.GetValue(ModeProperty);
    public static void SetMode(Button button, ThemeMode? value) => button.SetValue(ModeProperty, value);

    /// <summary>Whether the app is showing its dark theme right now, chosen or followed from the system.</summary>
    public static bool IsDark => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    /// <summary>Sets the app theme by <paramref name="next"/>; a value that is not a <see cref="ThemeMode"/> changes nothing. Returns the theme now showing, <see cref="ThemeVariant.Dark"/> or <see cref="ThemeVariant.Light"/>.</summary>
    public static ThemeVariant Apply(ThemeMode next = ThemeMode.Toggle)
    {
        if (Application.Current is { } app)
        {
            ThemeVariant? variant = next switch
            {
                ThemeMode.Dark => ThemeVariant.Dark,
                ThemeMode.Light => ThemeVariant.Light,
                ThemeMode.System => ThemeVariant.Default,
                ThemeMode.Toggle => IsDark ? ThemeVariant.Light : ThemeVariant.Dark,
                _ => null,
            };
            if (variant is not null) UnseenTransitions.During(null, () => app.RequestedThemeVariant = variant);
        }
        return IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private static void OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button) ThemeSweep.Run(button, () => Apply(GetMode(button) ?? ThemeMode.Toggle));
    }
}

/// <summary>The web's <c>.ins-daynight</c>: a sky in a top bar island, a sun by day and a moon among stars by night. It is checked while the theme showing is dark, and a click sets the app theme by its <see cref="ThemeToggle.ModeProperty"/>, <see cref="ThemeMode.Toggle"/> when unset.</summary>
public class DayNight : ToggleButton
{
    public DayNight() => ActualThemeVariantChanged += (_, _) => Sync();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Sync();
        base.OnAttachedToVisualTree(e);
    }

    // The theme decides the state, so a click asks for a theme rather than flipping the check.
    protected override void Toggle() => ThemeSweep.Run(this, () => ThemeToggle.Apply(ThemeToggle.GetMode(this) ?? ThemeMode.Toggle));

    private void Sync() => SetCurrentValue(IsCheckedProperty, ActualThemeVariant == ThemeVariant.Dark);
}
