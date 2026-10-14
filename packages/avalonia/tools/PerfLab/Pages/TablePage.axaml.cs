using System.Globalization;
using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace PerfLab.Pages;

public partial class TablePage : UserControl
{
    public TablePage() : this("الطلبات", 1200) { }

    public TablePage(string title, int rows)
    {
        InitializeComponent();
        Head.Title = title;
        var c = Grid.Columns;
        c[0].CellText = o => ((Order)o!).Id.ToString(CultureInfo.InvariantCulture);
        c[1].CellText = o => ((Order)o!).Customer;
        c[2].CellText = o => ((Order)o!).City;
        c[3].CellText = o => ((Order)o!).Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        c[4].CellText = o => Sample.Money(((Order)o!).Amount);
        c[5].CellText = o => ((Order)o!).Items.ToString(CultureInfo.InvariantCulture);
        c[6].CellText = o => ((Order)o!).Status;
        c[7].CellText = o => ((Order)o!).Channel;
        c[8].CellText = o => ((Order)o!).Agent;
        c[9].CellText = o => ((Order)o!).Note;
        Grid.ItemsSource = Sample.Orders(Program.Option("--rows") is { Length: > 0 } cap ? Math.Min(rows, int.Parse(cap)) : rows);
    }
}
