using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Tests;

public class ApiSurfaceTests
{
    private static readonly string[] ReviewedTypes =
    [
        "Brand.BrandDerivation", "Brand.BrandSet", "Controls.Alert", "Controls.ArabicMatch", "Controls.Autosave", "Controls.AutosaveEventArgs", "Controls.Avatar", "Controls.AvatarGroup", "Controls.Badge", "Controls.BirthChangedEventArgs", "Controls.BirthDateField", "Controls.BirthLayout", "Controls.Breadcrumb", "Controls.BreadcrumbClickEventArgs", "Controls.BreadcrumbItem", "Controls.BulkBar", "Controls.CalendarKind", "Controls.CaptionButtons", "Controls.CaptionPreview", "Controls.CarouselOptions", "Controls.Chart", "Controls.ChartSeries", "Controls.ChartType", "Controls.Chip", "Controls.Choice", "Controls.ChoiceGrid", "Controls.ChoiceMatrix", "Controls.Collapse", "Controls.ColorCommittedEventArgs", "Controls.ColorField", "Controls.CommandPalette", "Controls.Comment", "Controls.Countdown", "Controls.CountdownEventArgs", "Controls.CountdownState", "Controls.DateChangedEventArgs", "Controls.DateField", "Controls.DatePickMode", "Controls.DateTimeGroup", "Controls.DayNight", "Controls.Delta", "Controls.DescriptionItem", "Controls.DescriptionList", "Controls.Dialog", "Controls.DialogClosedBy", "Controls.DialogClosedEventArgs", "Controls.DialogFoot", "Controls.Dismiss", "Controls.Divider", "Controls.DocumentTab", "Controls.DocumentTabEventArgs", "Controls.DocumentTabMoveEventArgs", "Controls.DocumentTabs", "Controls.DrawerHost", "Controls.DrawerMode", "Controls.DrawerSide", "Controls.EmptyState", "Controls.Entrance", "Controls.EntranceEffect", "Controls.FileAddedEventArgs", "Controls.FileEntry", "Controls.FileUpload", "Controls.FileUploadStatus", "Controls.Find", "Controls.FindBar", "Controls.Icon", "Controls.InputCount", "Controls.InputCountMode", "Controls.InputCountState", "Controls.LoadingSpinner", "Controls.Mark", "Controls.MatrixColumn", "Controls.MatrixRow", "Controls.Money", "Controls.MonthCalendar", "Controls.MonthCalendarView", "Controls.MonthEvent", "Controls.MonthEventTone", "Controls.Motion", "Controls.Note", "Controls.Notify", "Controls.NotifyItem", "Controls.NotifyReadEventArgs", "Controls.OtpBox", "Controls.OtpCompletedEventArgs", "Controls.PageHead", "Controls.Pagination", "Controls.PaletteChoosingEventArgs", "Controls.PaletteItem", "Controls.PhoneBox", "Controls.PhoneChangedEventArgs", "Controls.Pill", "Controls.PopCard", "Controls.PopMenu", "Controls.ProgressRing", "Controls.Prose", "Controls.ProseList", "Controls.Ref", "Controls.SaveState", "Controls.SaveStatus", "Controls.ScrollspyEventArgs", "Controls.Segmented", "Controls.SegmentedItem", "Controls.Shell", "Controls.Shimmer", "Controls.SidebarChangedEventArgs", "Controls.SidebarState", "Controls.Skeleton", "Controls.SortDirection", "Controls.Split", "Controls.SplitShareEventArgs", "Controls.StatusBar", "Controls.StatusButton", "Controls.StatusItem", "Controls.Steps", "Controls.StepsItem", "Controls.Table", "Controls.TableCell", "Controls.TableColumn", "Controls.TableEdit", "Controls.TableEditEventArgs", "Controls.TableRow", "Controls.TableRowEventArgs", "Controls.TableSelectionEventArgs", "Controls.TableSelectionMode", "Controls.TableSort", "Controls.TableSortingEventArgs", "Controls.TabLink", "Controls.TagsChangedEventArgs", "Controls.TagsInput", "Controls.TagsOption", "Controls.Taskbar", "Controls.ThemeMode", "Controls.ThemeToggle", "Controls.TimeField", "Controls.Timeline", "Controls.TimelineDay", "Controls.TimelineItem", "Controls.TimeMode", "Controls.TitleBar", "Controls.Toast", "Controls.ToastClose", "Controls.ToastTone", "Controls.Toc", "Controls.TocLink", "Controls.Toolbar", "Controls.ToolbarButton", "Controls.ToolbarOverflow", "Controls.ToolbarSplitButton", "Controls.ToolbarToggleButton", "Controls.Tree", "Controls.Validate", "Controls.WindowActivity", "Controls.Wizard", "Controls.WizardAction", "Controls.WizardFinishedEventArgs", "Controls.WizardLeavingEventArgs", "Controls.WizardMotion", "Controls.WizardMovedEventArgs", "Controls.WizardPanel", "Controls.WizardStepsLook", "Frost", "InsiyabTheme",
    ];

    private static IEnumerable<Type> Exported() =>
        typeof(InsiyabTheme).Assembly.GetExportedTypes().Where(t => t.Namespace is not null && t.Namespace.StartsWith("Insiyab.Avalonia", StringComparison.Ordinal));

    [AvaloniaFact]
    public void Public_types_are_the_reviewed_set()
    {
        var actual = Exported().Select(t => t.FullName!["Insiyab.Avalonia.".Length..]).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        var expected = ReviewedTypes.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, actual);
    }

    [AvaloniaFact]
    public void Public_fields_are_avalonia_properties_or_pseudo_classes()
    {
        var wrong = new List<string>();
        foreach (var type in Exported().Where(t => !t.IsEnum))
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var isProperty = field.IsStatic && field.IsInitOnly && (typeof(AvaloniaProperty).IsAssignableFrom(field.FieldType) || typeof(RoutedEvent).IsAssignableFrom(field.FieldType));
                var isPseudoClass = field.IsLiteral && field.FieldType == typeof(string) && field.Name.EndsWith("PseudoClass", StringComparison.Ordinal);
                if (!isProperty && !isPseudoClass)
                    wrong.Add($"{type.Name}.{field.Name}");
            }
        }
        Assert.Empty(wrong.Order(StringComparer.Ordinal));
    }

    [AvaloniaFact]
    public void Event_args_are_made_by_the_library()
    {
        var open = Exported()
            .Where(t => typeof(EventArgs).IsAssignableFrom(t))
            .Where(t => t.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0)
            .Select(t => t.Name)
            .Order(StringComparer.Ordinal);
        Assert.Empty(open);
    }

    [AvaloniaFact]
    public void Attached_property_holders_are_static()
    {
        foreach (var type in new[] { typeof(Toast), typeof(Frost), typeof(Validate), typeof(InputCount), typeof(Taskbar) })
            Assert.True(type.IsAbstract && type.IsSealed, type.Name);
    }

    [AvaloniaFact]
    public void One_xml_namespace_reaches_the_theme_and_the_controls()
    {
        var panel = global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Parse<StackPanel>(
            """
            <StackPanel xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:ins="https://github.com/Mohammad-Diab/insiyab-ui">
                <ins:Pill Content="a" />
                <ContentControl Content="{x:Type ins:InsiyabTheme}" />
            </StackPanel>
            """, typeof(InsiyabTheme).Assembly);
        Assert.IsType<Pill>(panel.Children[0]);
        Assert.Equal(typeof(InsiyabTheme), Assert.IsType<ContentControl>(panel.Children[1]).Content);
    }
}
