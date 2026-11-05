using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class FilePickOnlyTests
{
    private static FileEntry Entry(string name, long size = 1000) => new(name, size);

    [AvaloniaFact]
    public void A_pick_only_upload_hands_over_each_pick_and_keeps_no_list()
    {
        var upload = new FileUpload { Multiple = true, ShowList = false, Accept = ".pdf" };
        var changes = 0;
        var added = new List<string>();
        upload.FilesChanged += (_, _) => changes++;
        upload.FileAdded += (_, e) => added.Add(e.Entry.Name);
        Show(new StackPanel { Margin = new Thickness(20), Children = { upload } }, FlowDirection.RightToLeft, width: 520, height: 400);
        var zoneHeight = upload.Bounds.Height;

        upload.AddFiles([Entry("a.pdf"), Entry("b.pdf", 2000), Entry("c.txt")]);
        Settle(100);
        Assert.Equal(1, changes);
        Assert.Equal(["a.pdf", "b.pdf"], added);
        Assert.Equal(["a.pdf", "b.pdf"], upload.Files.Select(f => f.Name).ToArray());
        Assert.Single(upload.Issues);
        Assert.Empty(upload.GetVisualDescendants().OfType<FileItem>());
        Assert.DoesNotContain(":has-files", upload.Classes);
        Assert.False(Part<Panel>(upload, "PART_List").IsVisible);

        upload.AddFiles([Entry("a.pdf")]);
        Assert.Equal(2, changes);
        Assert.Equal(["a.pdf"], upload.Files.Select(f => f.Name).ToArray());
        Assert.Equal(["a.pdf", "b.pdf", "a.pdf"], added);
        Settle(100);
        Assert.Empty(upload.Issues);
        Assert.Equal(zoneHeight, upload.Bounds.Height, 0.5);
    }

    [AvaloniaFact]
    public void Turning_the_list_back_on_lists_the_last_pick()
    {
        var upload = new FileUpload { Multiple = true, ShowList = false };
        Show(new StackPanel { Children = { upload } }, FlowDirection.RightToLeft, width: 520, height: 400);
        upload.AddFiles([Entry("a.pdf")]);
        upload.ShowList = true;
        Settle(100);
        Assert.Single(upload.GetVisualDescendants().OfType<FileItem>());
        Assert.Contains(":has-files", upload.Classes);
    }
}
