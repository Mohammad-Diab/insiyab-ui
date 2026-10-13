using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>Carries a <see cref="SplitButton"/>'s look classes (<c>primary</c>, <c>ghost</c>, <c>sm</c>…) onto the two buttons inside its template, so each half wears the library's button look; an unchecked <see cref="ToggleSplitButton"/> passes on its size only, so it stays the neutral button until it is checked.</summary>
public static class SplitFace
{
    /// <summary>Set by the split button's theme on its template's buttons.</summary>
    public static readonly AttachedProperty<bool> MirrorProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("Mirror", typeof(SplitFace));

    private static readonly AttachedProperty<Link?> LinkProperty =
        AvaloniaProperty.RegisterAttached<Button, Link?>("Link", typeof(SplitFace));

    internal static readonly string[] Tones =
        ["primary", "secondary", "success", "danger", "warning", "info", "ghost", "ghost-danger", "ghost-success", "ghost-warning", "ghost-info", "bare"];

    private static readonly string[] Sizes = ["sm", "lg"];

    static SplitFace()
    {
        MirrorProperty.Changed.AddClassHandler<Button>((b, e) =>
        {
            b.AttachedToVisualTree -= OnAttached;
            b.DetachedFromVisualTree -= OnDetached;
            if (!e.GetNewValue<bool>())
            {
                Unlink(b);
                return;
            }
            b.AttachedToVisualTree += OnAttached;
            b.DetachedFromVisualTree += OnDetached;
            if (TopLevel.GetTopLevel(b) is not null) Relink(b);
        });
    }

    public static bool GetMirror(Button button) => button.GetValue(MirrorProperty);

    public static void SetMirror(Button button, bool value) => button.SetValue(MirrorProperty, value);

    /// <summary>The tone the halves wear, set on the split button for its theme: a tone class, <c>on</c> for a checked toggle with none, or empty for the neutral button.</summary>
    public static readonly AttachedProperty<string> ToneProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string>("Tone", typeof(SplitFace), "");

    public static string GetTone(StyledElement owner) => owner.GetValue(ToneProperty);

    /// <summary>The tone a split button shows: an unchecked toggle split is the neutral button (or stays bare), as a ToggleButton is.</summary>
    internal static string ToneOf(StyledElement owner)
    {
        var tone = Tones.FirstOrDefault(owner.Classes.Contains) ?? "";
        return owner switch
        {
            ToggleSplitButton { IsChecked: false } => tone == "bare" ? "bare" : "",
            ToggleSplitButton { IsChecked: true } when tone.Length == 0 => "on",
            _ => tone,
        };
    }

    /// <summary>The classes a half wears for its split button.</summary>
    internal static IEnumerable<string> ClassesFor(StyledElement owner)
    {
        foreach (var c in owner.Classes)
            if (Sizes.Contains(c)) yield return c;
        var tone = ToneOf(owner);
        if (tone.Length > 0) yield return tone;
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Button b) Relink(b);
    }

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Button b) Unlink(b);
    }

    private static void Relink(Button half)
    {
        Unlink(half);
        if (half.TemplatedParent is not StyledElement owner) return;
        var link = new Link(half, owner);
        half.SetValue(LinkProperty, link);
        link.Sync();
    }

    private static void Unlink(Button half)
    {
        half.GetValue(LinkProperty)?.Dispose();
        half.ClearValue(LinkProperty);
    }

    private sealed class Link : IDisposable
    {
        private readonly Button _half;
        private readonly StyledElement _owner;
        private readonly HashSet<string> _added = [];

        public Link(Button half, StyledElement owner)
        {
            _half = half;
            _owner = owner;
            owner.Classes.CollectionChanged += OnClasses;
            owner.PropertyChanged += OnOwnerChanged;
        }

        private void OnClasses(object? sender, NotifyCollectionChangedEventArgs e) => Sync();

        private void OnOwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ToggleSplitButton.IsCheckedProperty) Sync();
        }

        public void Sync()
        {
            _owner.SetValue(ToneProperty, ToneOf(_owner));
            var want = ClassesFor(_owner).ToHashSet();
            foreach (var c in _added.Where(c => !want.Contains(c)).ToList())
            {
                _half.Classes.Remove(c);
                _added.Remove(c);
            }
            foreach (var c in want.Where(c => !_added.Contains(c)))
            {
                if (!_half.Classes.Contains(c)) _half.Classes.Add(c);
                _added.Add(c);
            }
        }

        public void Dispose()
        {
            _owner.Classes.CollectionChanged -= OnClasses;
            _owner.PropertyChanged -= OnOwnerChanged;
            foreach (var c in _added) _half.Classes.Remove(c);
            _added.Clear();
        }
    }
}
