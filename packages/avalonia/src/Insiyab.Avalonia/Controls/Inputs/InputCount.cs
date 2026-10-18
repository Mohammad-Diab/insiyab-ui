using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>What <see cref="InputCount"/> counts.</summary>
public enum InputCountMode
{
    /// <summary>No counter.</summary>
    None,
    /// <summary>Characters, against the field's <c>MaxLength</c> (or <see cref="InputCount.MaxProperty"/>).</summary>
    Chars,
    /// <summary>Words, against <see cref="InputCount.MaxProperty"/>.</summary>
    Words,
}

/// <summary>How close a counted field is to its limit.</summary>
public enum InputCountState
{
    /// <summary>Well under the limit, or no limit.</summary>
    Normal,
    /// <summary>At 90% of the limit or more, and not past it.</summary>
    Near,
    /// <summary>Past the limit.</summary>
    Over,
}

/// <summary>The web's <c>data-ins-count</c>: a counter under a <see cref="TextBox"/> of what has been written, amber near the limit and red past it; past a word limit the field is in error.</summary>
public static class InputCount
{
    public static readonly AttachedProperty<InputCountMode> ModeProperty =
        AvaloniaProperty.RegisterAttached<TextBox, InputCountMode>("Mode", typeof(InputCount));

    public static readonly AttachedProperty<int> MaxProperty =
        AvaloniaProperty.RegisterAttached<TextBox, int>("Max", typeof(InputCount));

    /// <summary>Your own wording, with <c>{n}</c> and <c>{max}</c>, as the web's <c>data-ins-text</c>.</summary>
    public static readonly AttachedProperty<string?> FormatProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string?>("Format", typeof(InputCount));

    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string?>("Text", typeof(InputCount));

    public static readonly AttachedProperty<InputCountState> StateProperty =
        AvaloniaProperty.RegisterAttached<TextBox, InputCountState>("State", typeof(InputCount));

    private static readonly AttachedProperty<string?> OwnErrorProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string?>("OwnError", typeof(InputCount));

    static InputCount()
    {
        ModeProperty.Changed.AddClassHandler<TextBox>((box, _) => Sync(box));
        MaxProperty.Changed.AddClassHandler<TextBox>((box, _) => Sync(box));
        FormatProperty.Changed.AddClassHandler<TextBox>((box, _) => Sync(box));
        TextBox.TextProperty.Changed.AddClassHandler<TextBox>((box, _) => { if (GetMode(box) != InputCountMode.None) Sync(box); });
        TextBox.MaxLengthProperty.Changed.AddClassHandler<TextBox>((box, _) => { if (GetMode(box) != InputCountMode.None) Sync(box); });
    }

    public static InputCountMode GetMode(TextBox box) => box.GetValue(ModeProperty);
    public static void SetMode(TextBox box, InputCountMode value) => box.SetValue(ModeProperty, value);
    public static int GetMax(TextBox box) => box.GetValue(MaxProperty);
    public static void SetMax(TextBox box, int value) => box.SetValue(MaxProperty, value);
    public static string? GetFormat(TextBox box) => box.GetValue(FormatProperty);
    public static void SetFormat(TextBox box, string? value) => box.SetValue(FormatProperty, value);
    public static string? GetText(TextBox box) => box.GetValue(TextProperty);
    public static InputCountState GetState(TextBox box) => box.GetValue(StateProperty);

    /// <summary>How many characters or words <paramref name="text"/> holds, as the counter counts them.</summary>
    internal static int Measure(string? text, InputCountMode mode)
    {
        var v = text ?? "";
        if (mode != InputCountMode.Words) return v.Length;
        return v.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static void Sync(TextBox box)
    {
        var mode = GetMode(box);
        if (mode == InputCountMode.None)
        {
            box.ClearValue(TextProperty);
            box.ClearValue(StateProperty);
            SetOwnError(box, null);
            return;
        }
        var words = mode == InputCountMode.Words;
        var n = Measure(box.Text, mode);
        var max = words ? GetMax(box) : box.MaxLength > 0 ? box.MaxLength : GetMax(box);
        var own = GetFormat(box);
        var text = own is not null ? own.Replace("{n}", n.ToString()).Replace("{max}", max > 0 ? max.ToString() : "")
            : words ? (max > 0 ? Words(n, max) : WordsFree(n)) : (max > 0 ? $"{n}/{max}" : n.ToString());
        box.SetValue(TextProperty, text);
        box.SetValue(StateProperty, max > 0 && n > max ? InputCountState.Over : max > 0 && n >= max * .9 ? InputCountState.Near : InputCountState.Normal);
        SetOwnError(box, words && max > 0 && n > max ? TooManyWords(max) : null);
    }

    // The error is ours alone to clear, so one set by the app is never taken away.
    private static void SetOwnError(TextBox box, string? message)
    {
        var had = box.GetValue(OwnErrorProperty);
        if (had == message) return;
        var errors = DataValidationErrors.GetErrors(box)?.Cast<object>().ToList() ?? [];
        if (had is not null) errors.Remove(had);
        if (message is not null) errors.Add(message);
        box.SetValue(OwnErrorProperty, message);
        if (errors.Count == 0) DataValidationErrors.ClearErrors(box);
        else DataValidationErrors.SetErrors(box, errors);
    }

    internal static string ArCount(int n, string[] w)
    {
        var r = n % 100;
        return n == 1 ? w[0] : n == 2 ? w[1] : n + " " + (r >= 3 && r <= 10 ? w[2] : r >= 11 ? w[3] : w[4]);
    }

    private static string Words(int n, int max) => $"{n}/{max} " + (max % 100 >= 3 && max % 100 <= 10 ? "كلمات" : "كلمة");

    private static string WordsFree(int n) => ArCount(n, ["كلمة واحدة", "كلمتان", "كلمات", "كلمة", "كلمة"]);

    private static string TooManyWords(int max) => "لا تتجاوز " + ArCount(max, ["كلمة واحدة", "كلمتين", "كلمات", "كلمة", "كلمة"]) + ".";
}
