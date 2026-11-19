using Avalonia;

namespace Insiyab.Avalonia.Controls;

/// <summary>A larger finger's box for the controls inside an element: under a finger every press reaches <see cref="SizeProperty"/> wide and tall around it (a row of controls drawn 44 high whose presses reach Material's 48), where the library's own reach is 44. Inherited; unset (0), or smaller than 44, the library's reach stands. A mouse finds each control exactly as drawn, and nothing moves.</summary>
public static class TouchTarget
{
    /// <summary>The box a press reaches under a finger, in device-independent pixels; at least the library's 44.</summary>
    public static readonly AttachedProperty<double> SizeProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("Size", typeof(TouchTarget), inherits: true);

    public static double GetSize(Visual element) => element.GetValue(SizeProperty);

    public static void SetSize(Visual element, double value) => element.SetValue(SizeProperty, value);
}
