using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>data-ins-theme-toggle</c> and <c>Insiyab.theme()</c>: the app-wide theme, light, dark or following the system.</summary>
public static class ThemeToggle
{
    /// <summary>On a Button: a click sets the app theme through a <see cref="ThemeSweep"/> from the button. "toggle" flips light and dark; "dark", "light" or "system" sets that one.</summary>
    public static readonly AttachedProperty<string?> ModeProperty =
        AvaloniaProperty.RegisterAttached<Button, string?>("Mode", typeof(ThemeToggle));

    static ThemeToggle()
    {
        ModeProperty.Changed.AddClassHandler<Button>((button, e) =>
        {
            button.Click -= OnClick;
            if (e.GetNewValue<string?>() is not null && button is not DayNight) button.Click += OnClick;
        });
    }

    public static string? GetMode(Button button) => button.GetValue(ModeProperty);
    public static void SetMode(Button button, string? value) => button.SetValue(ModeProperty, value);

    /// <summary>Whether the app is showing its dark theme right now, chosen or followed from the system.</summary>
    public static bool IsDark => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    /// <summary>Sets the app theme: "dark", "light", "system" or "toggle"; anything else changes nothing. Returns the theme now showing, "dark" or "light".</summary>
    public static string Apply(string next = "toggle")
    {
        if (Application.Current is { } app)
        {
            ThemeVariant? variant = next switch
            {
                "dark" => ThemeVariant.Dark,
                "light" => ThemeVariant.Light,
                "system" => ThemeVariant.Default,
                "toggle" => IsDark ? ThemeVariant.Light : ThemeVariant.Dark,
                _ => null,
            };
            if (variant is not null) app.RequestedThemeVariant = variant;
        }
        return IsDark ? "dark" : "light";
    }

    private static void OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button) ThemeSweep.Run(button, () => Apply(GetMode(button) ?? "toggle"));
    }
}

/// <summary>The web's <c>.ins-daynight</c>: a sky in a top bar island, a sun by day and a moon among stars by night. It is checked while the theme showing is dark, and a click sets the app theme by its <see cref="ThemeToggle.ModeProperty"/>, "toggle" when unset.</summary>
public class DayNight : ToggleButton
{
    public DayNight() => ActualThemeVariantChanged += (_, _) => Sync();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Sync();
        base.OnAttachedToVisualTree(e);
    }

    // The theme decides the state, so a click asks for a theme rather than flipping the check.
    protected override void Toggle() => ThemeSweep.Run(this, () => ThemeToggle.Apply(ThemeToggle.GetMode(this) ?? "toggle"));

    private void Sync() => SetCurrentValue(IsCheckedProperty, ActualThemeVariant == ThemeVariant.Dark);
}
