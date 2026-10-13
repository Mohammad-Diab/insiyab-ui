using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>
/// The web's <c>data-ins-validate</c>: rules on a <see cref="TextBox"/> (<c>Required</c>, <c>Email</c>, <c>MinLength</c>, <c>Pattern</c>), checked once the person leaves a field they changed or presses a <c>Submit</c> button in the <c>Form</c>, with the message written under the field.
/// </summary>
public sealed class Validate : AvaloniaObject
{
    /// <summary>Marks a container as the form whose fields a <see cref="SubmitProperty"/> button checks.</summary>
    public static readonly AttachedProperty<bool> FormProperty =
        AvaloniaProperty.RegisterAttached<Validate, Control, bool>("Form");

    /// <summary>On a <see cref="Button"/>: checks the form first, and stops the click (and its command) while any field is invalid.</summary>
    public static readonly AttachedProperty<bool> SubmitProperty =
        AvaloniaProperty.RegisterAttached<Validate, Button, bool>("Submit");

    public static readonly AttachedProperty<bool> RequiredProperty =
        AvaloniaProperty.RegisterAttached<Validate, TextBox, bool>("Required");

    public static readonly AttachedProperty<bool> EmailProperty =
        AvaloniaProperty.RegisterAttached<Validate, TextBox, bool>("Email");

    public static readonly AttachedProperty<int> MinLengthProperty =
        AvaloniaProperty.RegisterAttached<Validate, TextBox, int>("MinLength");

    /// <summary>A regular expression the whole value must match, as HTML's <c>pattern</c>.</summary>
    public static readonly AttachedProperty<string?> PatternProperty =
        AvaloniaProperty.RegisterAttached<Validate, TextBox, string?>("Pattern");

    /// <summary>Your own message, shown in place of the library's for any rule the field breaks.</summary>
    public static readonly AttachedProperty<string?> MessageProperty =
        AvaloniaProperty.RegisterAttached<Validate, TextBox, string?>("Message");

    /// <summary>Raised on the form when a submit button finds every field valid.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> SubmittedEvent =
        RoutedEvent.Register<Validate, RoutedEventArgs>("Submitted", RoutingStrategies.Bubble);

    private static readonly AttachedProperty<bool> DirtyProperty =
        AvaloniaProperty.RegisterAttached<Validate, TextBox, bool>("Dirty");

    private static readonly AttachedProperty<bool> ReportedProperty =
        AvaloniaProperty.RegisterAttached<Validate, TextBox, bool>("Reported");

    private static readonly AttachedProperty<string?> OwnErrorProperty =
        AvaloniaProperty.RegisterAttached<Validate, TextBox, string?>("OwnError");

    private static readonly Regex EmailShape = new(@"^[^@\s]+@[^@\s]+$", RegexOptions.CultureInvariant);

    static Validate()
    {
        TextBox.TextProperty.Changed.AddClassHandler<TextBox>(OnText);
        InputElement.LostFocusEvent.AddClassHandler<TextBox>((box, _) => OnLeave(box));
        Button.ClickEvent.AddClassHandler<Button>(OnClick);
    }

    private Validate() { }

    public static bool GetForm(Control c) => c.GetValue(FormProperty);
    public static void SetForm(Control c, bool v) => c.SetValue(FormProperty, v);
    public static bool GetSubmit(Button c) => c.GetValue(SubmitProperty);
    public static void SetSubmit(Button c, bool v) => c.SetValue(SubmitProperty, v);
    public static bool GetRequired(TextBox c) => c.GetValue(RequiredProperty);
    public static void SetRequired(TextBox c, bool v) => c.SetValue(RequiredProperty, v);
    public static bool GetEmail(TextBox c) => c.GetValue(EmailProperty);
    public static void SetEmail(TextBox c, bool v) => c.SetValue(EmailProperty, v);
    public static int GetMinLength(TextBox c) => c.GetValue(MinLengthProperty);
    public static void SetMinLength(TextBox c, int v) => c.SetValue(MinLengthProperty, v);
    public static string? GetPattern(TextBox c) => c.GetValue(PatternProperty);
    public static void SetPattern(TextBox c, string? v) => c.SetValue(PatternProperty, v);
    public static string? GetMessage(TextBox c) => c.GetValue(MessageProperty);
    public static void SetMessage(TextBox c, string? v) => c.SetValue(MessageProperty, v);
    public static void AddSubmittedHandler(Control c, EventHandler<RoutedEventArgs> handler) => c.AddHandler(SubmittedEvent, handler);
    public static void RemoveSubmittedHandler(Control c, EventHandler<RoutedEventArgs> handler) => c.RemoveHandler(SubmittedEvent, handler);

    /// <summary>What is wrong with the field's value, or null when nothing is.</summary>
    public static string? Problem(TextBox box)
    {
        var v = box.Text ?? "";
        string? found = null;
        if (GetRequired(box) && v.Trim().Length == 0) found = "املأ هذا الحقل.";
        else if (v.Length == 0) return null;
        else if (box is OtpBox { ValidationMessage: { } partial }) found = partial;
        else if (GetEmail(box) && !EmailShape.IsMatch(v)) found = "اكتب بريدًا إلكترونيًا صحيحًا.";
        else if (GetMinLength(box) is var min and > 0 && v.Length < min)
            found = "اكتب " + InputCount.ArCount(min, ["حرفًا واحدًا", "حرفين", "أحرف", "حرفًا", "حرف"]) + " على الأقل.";
        else if (GetPattern(box) is { Length: > 0 } pattern && !Regex.IsMatch(v, "^(?:" + pattern + ")$", RegexOptions.CultureInvariant))
            found = "اكتب القيمة بالصيغة المطلوبة.";
        return found is null ? null : GetMessage(box) ?? found;
    }

    /// <summary>Checks every field in <paramref name="form"/>, shows each message, focuses the first invalid field, and says whether all were valid.</summary>
    public static bool Check(Control form)
    {
        TextBox? first = null;
        foreach (var box in form.GetVisualDescendants().OfType<TextBox>())
            if (!Report(box)) first ??= box;
        first?.Focus();
        return first is null;
    }

    /// <summary>The web's form reset for the checks: every field in <paramref name="form"/> drops its message and waits again to be left changed or submitted.</summary>
    public static void Reset(Control form)
    {
        foreach (var box in form.GetLogicalDescendants().OfType<TextBox>())
        {
            if (!HasRules(box)) continue;
            box.ClearValue(DirtyProperty);
            box.ClearValue(ReportedProperty);
            SetOwnError(box, null);
        }
    }

    /// <summary>Checks one field as a submit does and shows its message: true when it has no rule or breaks none.</summary>
    internal static bool Report(TextBox box)
    {
        if (!HasRules(box)) return true;
        box.SetValue(ReportedProperty, true);
        var problem = Problem(box);
        SetOwnError(box, problem);
        return problem is null;
    }

    private static bool HasRules(TextBox box) =>
        GetRequired(box) || GetEmail(box) || GetMinLength(box) > 0 || !string.IsNullOrEmpty(GetPattern(box));

    private static void OnText(TextBox box, AvaloniaPropertyChangedEventArgs e)
    {
        if (!HasRules(box)) return;
        if (box.IsFocused || box.IsKeyboardFocusWithin) box.SetValue(DirtyProperty, true);
        if (box.GetValue(ReportedProperty)) SetOwnError(box, Problem(box));
    }

    private static void OnLeave(TextBox box)
    {
        if (!HasRules(box) || !box.GetValue(DirtyProperty)) return;
        box.SetValue(ReportedProperty, true);
        SetOwnError(box, Problem(box));
    }

    private static void OnClick(Button button, RoutedEventArgs e)
    {
        if (!GetSubmit(button)) return;
        var form = button.GetVisualAncestors().OfType<Control>().FirstOrDefault(GetForm);
        if (form is null) return;
        if (!Check(form))
        {
            e.Handled = true;
            return;
        }
        form.RaiseEvent(new RoutedEventArgs(SubmittedEvent, form));
    }

    private static void SetOwnError(TextBox box, string? message)
    {
        var had = box.GetValue(OwnErrorProperty);
        if (had == message) return;
        var errors = DataValidationErrors.GetErrors(box)?.Cast<object>().ToList() ?? [];
        if (had is not null) errors.Remove(had);
        if (message is not null) errors.Insert(0, message);
        box.SetValue(OwnErrorProperty, message);
        if (errors.Count == 0) DataValidationErrors.ClearErrors(box);
        else DataValidationErrors.SetErrors(box, errors);
    }
}
