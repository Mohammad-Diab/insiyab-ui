using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("chips", "الرقاقات والوسوم")]
public partial class ChipsPage : UserControl
{
    public ChipsPage()
    {
        InitializeComponent();
        ListTags.Values.Add("a");
        FreeTags.Values.Add("وسم");
        FreeListTags.Values.AddRange(["a", "قيمة حرة"]);
        MaxTags.Values.AddRange(["a", "b", "c"]);
    }
}
