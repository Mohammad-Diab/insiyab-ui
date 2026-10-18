using Avalonia.Media;

namespace Insiyab.Avalonia.Brand;

/// <summary>The brand tokens derived from one primary colour for one theme, as Insiyab.brand() derives them on the web.</summary>
public readonly record struct BrandSet
{
    public Color Primary { get; init; }
    public Color PrimaryDark { get; init; }
    public Color PrimaryDarker { get; init; }
    public Color PrimaryRgb { get; init; }
    public Color GradRgb { get; init; }
    public Color AccentRgb { get; init; }
    public Color OnPrimary { get; init; }
    public Color Wash1 { get; init; }
    public Color Wash2 { get; init; }
    public Color PrimaryMid { get; init; }
    public Color OnPrimaryMid { get; init; }
}
