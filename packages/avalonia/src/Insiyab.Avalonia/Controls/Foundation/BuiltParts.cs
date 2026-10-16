using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

// The Borders, TextBlocks and StackPanels the library builds in code: as with TemplateBorder, the app's Border.*, TextBlock.* and StackPanel.* styles and looks never reach them, and each control's parts carry only that control's own styles.

/// <summary>A Border that <see cref="MonthCalendar"/> builds; its styles name it <c>ins|MonthBorder</c>.</summary>
internal sealed class MonthBorder : Border
{
    protected override Type StyleKeyOverride => typeof(MonthBorder);
}

/// <summary>A TextBlock that <see cref="MonthCalendar"/> builds; its styles name it <c>ins|MonthText</c>.</summary>
internal sealed class MonthText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(MonthText);
}

/// <summary>A StackPanel that <see cref="MonthCalendar"/> builds; its styles name it <c>ins|MonthStack</c>.</summary>
internal sealed class MonthStack : StackPanel
{
    protected override Type StyleKeyOverride => typeof(MonthStack);
}

/// <summary>A Border that <see cref="BirthDateField"/> builds; its styles name it <c>ins|BirthBorder</c>.</summary>
internal sealed class BirthBorder : Border
{
    protected override Type StyleKeyOverride => typeof(BirthBorder);
}

/// <summary>A TextBlock that <see cref="BirthDateField"/> builds; its styles name it <c>ins|BirthText</c>.</summary>
internal sealed class BirthText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(BirthText);
}

/// <summary>A StackPanel that <see cref="BirthDateField"/> builds; its styles name it <c>ins|BirthStack</c>.</summary>
internal sealed class BirthStack : StackPanel
{
    protected override Type StyleKeyOverride => typeof(BirthStack);
}

/// <summary>A Border that <see cref="DateCalendar"/> and its switch turn build; its styles name it <c>ins|CalendarBorder</c>.</summary>
internal sealed class CalendarBorder : Border
{
    protected override Type StyleKeyOverride => typeof(CalendarBorder);
}

/// <summary>A TextBlock that <see cref="DateCalendar"/> builds; its styles name it <c>ins|CalendarText</c>.</summary>
internal sealed class CalendarText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(CalendarText);
}

/// <summary>A Border that <see cref="ChoiceMatrix"/> builds; its styles name it <c>ins|MatrixBorder</c>.</summary>
internal sealed class MatrixBorder : Border
{
    protected override Type StyleKeyOverride => typeof(MatrixBorder);
}

/// <summary>A TextBlock that <see cref="ChoiceMatrix"/> builds; its styles name it <c>ins|MatrixText</c>.</summary>
internal sealed class MatrixText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(MatrixText);
}

/// <summary>A Border that <see cref="CommandPalette"/> builds; its styles name it <c>ins|PaletteBorder</c>.</summary>
internal sealed class PaletteBorder : Border
{
    protected override Type StyleKeyOverride => typeof(PaletteBorder);
}

/// <summary>A TextBlock that <see cref="CommandPalette"/> builds; its styles name it <c>ins|PaletteText</c>.</summary>
internal sealed class PaletteText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(PaletteText);
}
