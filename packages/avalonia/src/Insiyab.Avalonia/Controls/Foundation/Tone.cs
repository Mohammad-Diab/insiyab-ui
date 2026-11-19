using Avalonia;

namespace Insiyab.Avalonia.Controls;

/// <summary>A tone class from data: <c>ins:Tone.Class="{Binding Tone}"</c> puts that class (<c>ok warn bad info brand mute</c>, or any its look knows) on the element and takes off the one it put before.</summary>
public static class Tone
{
    public static readonly AttachedProperty<string?> ClassProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("Class", typeof(Tone));

    static Tone() =>
        ClassProperty.Changed.AddClassHandler<StyledElement>((element, e) =>
        {
            if (e.GetOldValue<string?>()?.Trim() is { Length: > 0 } old) element.Classes.Remove(old);
            if (e.GetNewValue<string?>()?.Trim() is { Length: > 0 } now) element.Classes.Add(now);
        });

    public static string? GetClass(StyledElement element) => element.GetValue(ClassProperty);
    public static void SetClass(StyledElement element, string? value) => element.SetValue(ClassProperty, value);
}
