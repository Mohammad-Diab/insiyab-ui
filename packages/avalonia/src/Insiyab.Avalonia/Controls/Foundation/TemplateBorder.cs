using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The Border the library's control templates are built from. App-wide <c>Border.*</c> styles (<c>card</c>, <c>panel</c>, <c>island</c>…) never reach it: Avalonia gives every plain Border a watcher for each of those styles, so a template part that is a plain Border costs dozens of them for nothing. Template selectors name these parts as <c>:is(Border)#PART_…</c>.</summary>
internal class TemplateBorder : Border
{
    protected override Type StyleKeyOverride => typeof(TemplateBorder);
}
