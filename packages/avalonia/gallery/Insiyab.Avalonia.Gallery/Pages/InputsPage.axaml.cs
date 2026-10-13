using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("inputs", "الحقول")]
public partial class InputsPage : UserControl
{
    public InputsPage()
    {
        InitializeComponent();
        FlowVariants(this);
        DataValidationErrors.SetErrors(BadBox, ["رسالة الخطأ."]);
    }

    // Lays each "variants" grid's children out two to a row, each row as tall as its own content.
    internal static void FlowVariants(Control page)
    {
        foreach (var grid in page.GetLogicalDescendants().OfType<Grid>().Where(g => g.Classes.Contains("variants")))
        {
            var columns = Math.Max(1, grid.ColumnDefinitions.Count);
            var rows = (grid.Children.Count + columns - 1) / columns;
            grid.RowDefinitions.Clear();
            for (var r = 0; r < rows; r++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (var i = 0; i < grid.Children.Count; i++)
            {
                Grid.SetRow(grid.Children[i], i / columns);
                Grid.SetColumn(grid.Children[i], i % columns);
            }
        }
    }
}
