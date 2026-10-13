using Avalonia.Media;

namespace Insiyab.Avalonia.Brand;

/// <summary>The brand tokens derived from one primary colour for one theme, as Insiyab.brand() derives them on the web.</summary>
public readonly record struct BrandSet(
    Color Primary,
    Color PrimaryDark,
    Color PrimaryDarker,
    Color PrimaryRgb,
    Color GradRgb,
    Color AccentRgb,
    Color OnPrimary,
    Color Wash1,
    Color Wash2,
    Color PrimaryMid,
    Color OnPrimaryMid);
