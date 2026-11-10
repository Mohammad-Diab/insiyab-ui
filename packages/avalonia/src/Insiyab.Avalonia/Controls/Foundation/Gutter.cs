using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Insiyab.Avalonia.Controls;

/// <summary>The sides of a <see cref="Gutter"/>: <c>Start</c> and <c>End</c> follow the reading direction; combine them with commas in XAML (<c>"Inline,Bottom"</c>).</summary>
[Flags]
public enum GutterSides
{
    None = 0,
    Start = 1,
    Top = 2,
    End = 4,
    Bottom = 8,
    Inline = Start | End,
    Block = Top | Bottom,
    All = Start | Top | End | Bottom,
}

/// <summary>Room for shadows (<c>Shadow</c>, <c>InsSp4</c>) or focus rings only (<c>Ring</c>, <c>InsSp1</c>) on the named sides of the content right inside whatever clips at its edge, as its <c>Margin</c> at style priority.</summary>
public static class Gutter
{
    /// <summary>The sides that keep room for shadows (<c>InsSp4</c>). None by default.</summary>
    public static readonly AttachedProperty<GutterSides> ShadowProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, GutterSides>("Shadow", typeof(Gutter));

    /// <summary>The sides that keep room for a focus ring only (<c>InsSp1</c>). None by default.</summary>
    public static readonly AttachedProperty<GutterSides> RingProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, GutterSides>("Ring", typeof(Gutter));

    private const string ShadowKey = "InsSp4";
    private const string RingKey = "InsSp1";

    private sealed class State
    {
        public IDisposable? Margin;
        public IDisposable? ShadowWatch;
        public IDisposable? RingWatch;
        public double Shadow;
        public double Ring;
    }

    private static readonly ConditionalWeakTable<Layoutable, State> s_states = new();

    static Gutter()
    {
        ShadowProperty.Changed.AddClassHandler<Layoutable>((l, _) => Update(l));
        RingProperty.Changed.AddClassHandler<Layoutable>((l, _) => Update(l));
    }

    public static GutterSides GetShadow(Layoutable element) => element.GetValue(ShadowProperty);

    public static void SetShadow(Layoutable element, GutterSides value) => element.SetValue(ShadowProperty, value);

    public static GutterSides GetRing(Layoutable element) => element.GetValue(RingProperty);

    public static void SetRing(Layoutable element, GutterSides value) => element.SetValue(RingProperty, value);

    // Starts or stops following the two tokens, then writes the margin they give.
    private static void Update(Layoutable element)
    {
        var shadow = GetShadow(element);
        var ring = GetRing(element);
        var state = s_states.GetValue(element, _ => new State());
        if (shadow == GutterSides.None && ring == GutterSides.None)
        {
            state.ShadowWatch?.Dispose();
            state.RingWatch?.Dispose();
            state.Margin?.Dispose();
            s_states.Remove(element);
            return;
        }
        if (state.ShadowWatch is null)
        {
            state.ShadowWatch = element.GetResourceObservable(ShadowKey).Subscribe(new Watch(v => { state.Shadow = v; Apply(element, state); }));
            state.RingWatch = element.GetResourceObservable(RingKey).Subscribe(new Watch(v => { state.Ring = v; Apply(element, state); }));
        }
        Apply(element, state);
    }

    private static void Apply(Layoutable element, State state)
    {
        var shadow = GetShadow(element);
        var ring = GetRing(element);
        double Side(GutterSides side) => shadow.HasFlag(side) ? state.Shadow : ring.HasFlag(side) ? state.Ring : 0;
        // Avalonia mirrors a right-to-left layout, so the margin's left is the start side in either direction.
        var margin = new Thickness(Side(GutterSides.Start), Side(GutterSides.Top), Side(GutterSides.End), Side(GutterSides.Bottom));
        state.Margin?.Dispose();
        state.Margin = element.SetValue(Layoutable.MarginProperty, margin, BindingPriority.Style);
    }

    // Reads a spacing token as a number (a token missing from the tree is 0).
    private sealed class Watch(Action<double> changed) : IObserver<object?>
    {
        public void OnNext(object? value) => changed(value switch
        {
            double d => d,
            float f => f,
            int i => i,
            _ => 0,
        });

        public void OnCompleted() { }

        public void OnError(Exception error) { }
    }
}
