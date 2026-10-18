using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Insiyab.Avalonia.Controls;

/// <summary>A file was added to a <see cref="FileUpload"/>: the web's <c>ins:file</c>.</summary>
public class FileAddedEventArgs : RoutedEventArgs
{
    internal FileAddedEventArgs(RoutedEvent routedEvent, FileEntry entry, bool upload) : base(routedEvent)
    {
        Entry = entry;
        Upload = upload;
    }

    public FileEntry Entry { get; }

    /// <summary>True when the page is to upload it now and report through Progress, Done and Fail.</summary>
    public bool Upload { get; }
}

/// <summary>The file field, the web's <c>data-ins-file</c>: a zone to drop, paste or pick files into and the list of those chosen, within <see cref="Accept"/>, <see cref="MaxSize"/> and <see cref="MaxCount"/>. <c>Classes="compact"</c> is a button and the list.</summary>
[TemplatePart("PART_Zone", typeof(Border))]
[TemplatePart("PART_Issues", typeof(Panel))]
[TemplatePart("PART_List", typeof(Panel))]
[PseudoClasses(":over", ":has-files", ":invalid")]
public class FileUpload : TemplatedControl
{
    public static readonly StyledProperty<bool> MultipleProperty =
        AvaloniaProperty.Register<FileUpload, bool>(nameof(Multiple));

    public static readonly StyledProperty<string?> AcceptProperty =
        AvaloniaProperty.Register<FileUpload, string?>(nameof(Accept));

    public static readonly StyledProperty<string?> MaxSizeProperty =
        AvaloniaProperty.Register<FileUpload, string?>(nameof(MaxSize));

    public static readonly StyledProperty<int> MaxCountProperty =
        AvaloniaProperty.Register<FileUpload, int>(nameof(MaxCount));

    public static readonly StyledProperty<string?> NoteProperty =
        AvaloniaProperty.Register<FileUpload, string?>(nameof(Note));

    public static readonly StyledProperty<bool> UploadProperty =
        AvaloniaProperty.Register<FileUpload, bool>(nameof(Upload));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<FileUpload, string?>(nameof(Locale));

    public static readonly DirectProperty<FileUpload, string> DragTextProperty =
        AvaloniaProperty.RegisterDirect<FileUpload, string>(nameof(DragText), o => o.DragText);

    public static readonly DirectProperty<FileUpload, string> PickTextProperty =
        AvaloniaProperty.RegisterDirect<FileUpload, string>(nameof(PickText), o => o.PickText);

    public static readonly DirectProperty<FileUpload, string> NoteTextProperty =
        AvaloniaProperty.RegisterDirect<FileUpload, string>(nameof(NoteText), o => o.NoteText);

    public static readonly DirectProperty<FileUpload, string?> ValidationMessageProperty =
        AvaloniaProperty.RegisterDirect<FileUpload, string?>(nameof(ValidationMessage), o => o.ValidationMessage);

    public static readonly RoutedEvent<FileAddedEventArgs> FileAddedEvent =
        RoutedEvent.Register<FileUpload, FileAddedEventArgs>(nameof(FileAdded), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> FilesChangedEvent =
        RoutedEvent.Register<FileUpload, RoutedEventArgs>(nameof(FilesChanged), RoutingStrategies.Bubble);

    private readonly List<FileEntry> _files = [];
    private readonly Dictionary<FileEntry, FileItem> _items = [];
    private Panel? _issues, _list;
    private string _drag = string.Empty, _pick = string.Empty, _note = string.Empty;
    private string? _message;
    private int _depth;
    private Control? _zone;
    private bool _touch;

    static FileUpload()
    {
        FocusableProperty.OverrideDefaultValue<FileUpload>(true);
        DragDrop.AllowDropProperty.OverrideDefaultValue<FileUpload>(true);
    }

    public FileUpload()
    {
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
        UpdateTexts();
    }

    /// <summary>A new pick adds to the list rather than replacing it (the input's <c>multiple</c>).</summary>
    public bool Multiple { get => GetValue(MultipleProperty); set => SetValue(MultipleProperty, value); }

    /// <summary>The input's <c>accept</c>: <c>.pdf,image/*</c>; checked on drops and pastes too.</summary>
    public string? Accept { get => GetValue(AcceptProperty); set => SetValue(AcceptProperty, value); }

    /// <summary>The largest file, in B, KB, MB or GB (<c>data-ins-file-max="5MB"</c>).</summary>
    public string? MaxSize { get => GetValue(MaxSizeProperty); set => SetValue(MaxSizeProperty, value); }

    /// <summary>The most files, with <see cref="Multiple"/> (<c>data-ins-file-count</c>); 0 for no limit.</summary>
    public int MaxCount { get => GetValue(MaxCountProperty); set => SetValue(MaxCountProperty, value); }

    /// <summary>The zone's small print, in place of the one made from the limits (<c>data-ins-file-note</c>).</summary>
    public string? Note { get => GetValue(NoteProperty); set => SetValue(NoteProperty, value); }

    /// <summary>Each file added is the page's to upload on the spot (<c>data-ins-file-upload</c>); see <see cref="FileAdded"/>.</summary>
    public bool Upload { get => GetValue(UploadProperty); set => SetValue(UploadProperty, value); }

    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    public string DragText { get => _drag; private set => SetAndRaise(DragTextProperty, ref _drag, value); }
    public string PickText { get => _pick; private set => SetAndRaise(PickTextProperty, ref _pick, value); }
    public string NoteText { get => _note; private set => SetAndRaise(NoteTextProperty, ref _note, value); }

    /// <summary>"Wait for the uploads" or "remove the failed ones" while an upload is on its way or has failed.</summary>
    public string? ValidationMessage { get => _message; private set => SetAndRaise(ValidationMessageProperty, ref _message, value); }

    public IReadOnlyList<FileEntry> Files => _files;

    /// <summary>The issues the last addition reported: a type, a size or a count it refused.</summary>
    public IReadOnlyList<string> Issues { get; private set; } = [];

    public event EventHandler<FileAddedEventArgs>? FileAdded
    {
        add => AddHandler(FileAddedEvent, value);
        remove => RemoveHandler(FileAddedEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? FilesChanged
    {
        add => AddHandler(FilesChangedEvent, value);
        remove => RemoveHandler(FilesChangedEvent, value);
    }

    private bool Arabic => PluginText.IsArabic(this, Locale);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _issues = e.NameScope.Find<Panel>("PART_Issues");
        _list = e.NameScope.Find<Panel>("PART_List");
        if (_zone is not null) _zone.PointerReleased -= OnZoneReleased;
        _zone = e.NameScope.Find<Control>("PART_Zone");
        if (_zone is not null) _zone.PointerReleased += OnZoneReleased;
        Render();
        Report(Issues);
    }

    private void OnZoneReleased(object? sender, PointerReleasedEventArgs a)
    {
        if (a.InitialPressMouseButton == MouseButton.Left && IsEffectivelyEnabled) _ = PickAsync();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _touch = TopLevel.GetTopLevel(this) is not Window;
        UpdateTexts();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MultipleProperty || change.Property == MaxSizeProperty || change.Property == MaxCountProperty ||
            change.Property == NoteProperty || change.Property == LocaleProperty || change.Property == FlowDirectionProperty)
        {
            UpdateTexts();
            if (change.Property == LocaleProperty || change.Property == FlowDirectionProperty) Render();
        }
        else if (change.Property == UploadProperty) Check();
    }

    private void UpdateTexts()
    {
        var ar = Arabic;
        DragText = _touch ? string.Empty : Multiple ? (ar ? "اسحب الملفات إلى هنا أو" : "Drag files here or") : (ar ? "اسحب الملف إلى هنا أو" : "Drag a file here or");
        PickText = Multiple ? (ar ? "اختر ملفات" : "choose files") : (ar ? "اختر ملفًا" : "choose a file");
        if (Note is { } own) { NoteText = own; return; }
        var parts = new List<string>();
        var max = FileTypes.Bytes(MaxSize);
        if (max > 0) parts.Add(ar ? $"حتى {FileTypes.Size(max, true)} للملف الواحد" : $"up to {FileTypes.Size(max, false)} each");
        if (Multiple && MaxCount > 0) parts.Add(ar ? ArFiles(MaxCount) + " على الأكثر" : EnFiles(MaxCount) + " at most");
        NoteText = string.Join(ar ? "، " : ", ", parts);
    }

    private static string ArFiles(int n)
    {
        var r = n % 100;
        return n == 1 ? "ملف واحد" : n == 2 ? "ملفان" : n + (r >= 3 && r <= 10 ? " ملفات" : r >= 11 ? " ملفًا" : " ملف");
    }

    private static string EnFiles(int n) => n == 1 ? "1 file" : n + " files";

    /// <summary>Adds what came in, from the picker, a drop or a paste, within the limits, and says what was left out.</summary>
    public void AddFiles(IEnumerable<FileEntry> incoming)
    {
        var ar = Arabic;
        var issues = new List<string>();
        var max = FileTypes.Bytes(MaxSize);
        var count = Multiple ? MaxCount : 1;
        var next = Multiple ? _files.ToList() : [];
        var added = new List<FileEntry>();
        foreach (var f in incoming)
        {
            if (!FileTypes.Accepts(Accept, f.Name)) { issues.Add(ar ? $"نوع «{f.Name}» غير مقبول." : $"“{f.Name}” is not a type this field accepts."); continue; }
            if (max > 0 && f.Size > max) { issues.Add(ar ? $"«{f.Name}» أكبر من {FileTypes.Size(max, true)}." : $"“{f.Name}” is larger than {FileTypes.Size(max, false)}."); continue; }
            if (next.Any(x => x.Name == f.Name && x.Size == f.Size)) continue;
            if (!Multiple) { next = [f]; added = [f]; continue; }
            if (count > 0 && next.Count >= count) { issues.Add(ar ? $"لم يُضف «{f.Name}»: {ArFiles(count)} على الأكثر." : $"“{f.Name}” was not added: {EnFiles(count)} at most."); continue; }
            next.Add(f);
            added.Add(f);
        }
        _files.Clear();
        _files.AddRange(next);
        Render();
        Report(issues);
        foreach (var f in added) Announce(f);
        RaiseEvent(new RoutedEventArgs(FilesChangedEvent));
    }

    /// <summary>Empties the list, or replaces it with these files within the limits, quietly: <c>Insiyab.file(field, files)</c>.</summary>
    public void SetFiles(IEnumerable<FileEntry>? files)
    {
        _files.Clear();
        var list = files?.ToList() ?? [];
        if (list.Count > 0) AddFiles(list);
        else { Render(); Report([]); }
    }

    public void Remove(FileEntry entry)
    {
        if (!_files.Remove(entry)) return;
        Render();
        Report([]);
        RaiseEvent(new RoutedEventArgs(FilesChangedEvent));
        Focus();
    }

    /// <summary>The page's upload got this far, 0 to 1 (null for not known); false once the file is no longer in the list.</summary>
    public bool Progress(FileEntry entry, double? fraction) => Update(entry, FileUploadStatus.Sending, e => e.Progress = fraction is { } f ? Math.Clamp(f, 0, 1) : null);

    public bool Fail(FileEntry entry, string? message) => Update(entry, FileUploadStatus.Failed, e => e.Message = message);

    public bool Done(FileEntry entry, string? id) => Update(entry, FileUploadStatus.Done, e => e.UploadId = id);

    private bool Update(FileEntry entry, FileUploadStatus status, Action<FileEntry> apply)
    {
        if (!_files.Contains(entry)) return false;
        entry.Status = status;
        apply(entry);
        if (_items.TryGetValue(entry, out var item)) item.Refresh(Arabic);
        Check();
        return true;
    }

    private void Announce(FileEntry entry)
    {
        if (Upload)
        {
            entry.Status = FileUploadStatus.Sending;
            entry.Progress = null;
            if (_items.TryGetValue(entry, out var item)) item.Refresh(Arabic);
        }
        RaiseEvent(new FileAddedEventArgs(FileAddedEvent, entry, Upload));
        Check();
    }

    private void Render()
    {
        foreach (var item in _items.Values) item.RemoveRequested -= OnRemoveRequested;
        _items.Clear();
        _list?.Children.Clear();
        foreach (var f in _files)
        {
            var item = new FileItem(f, IsEffectivelyEnabled);
            item.RemoveRequested += OnRemoveRequested;
            item.Refresh(Arabic);
            _items[f] = item;
            _list?.Children.Add(item);
        }
        if (_list is not null) _list.IsVisible = _files.Count > 0;
        PseudoClasses.Set(":has-files", _files.Count > 0);
        Check();
    }

    private void OnRemoveRequested(object? sender, EventArgs e)
    {
        if (sender is FileItem item) Remove(item.Entry);
    }

    private void Report(IReadOnlyList<string> issues)
    {
        Issues = issues;
        if (_issues is null) return;
        _issues.Children.Clear();
        foreach (var issue in issues) _issues.Children.Add(new TextBlock { Text = issue, TextWrapping = TextWrapping.Wrap, Classes = { "file-issue" } });
        _issues.IsVisible = issues.Count > 0;
    }

    private void Check()
    {
        string? msg = null;
        if (Upload)
        {
            foreach (var f in _files)
            {
                if (f.Status == FileUploadStatus.Failed) { msg = Arabic ? "أزِل الملفات التي تعذر رفعها أو أعد رفعها." : "Remove the files that failed to upload, or upload them again."; break; }
                if (f.Status == FileUploadStatus.Sending) msg = Arabic ? "انتظر حتى يكتمل رفع الملفات." : "Wait for the files to finish uploading.";
            }
        }
        ValidationMessage = msg;
        PseudoClasses.Set(":invalid", Upload && _files.Any(f => f.Status == FileUploadStatus.Failed));
    }

    /// <summary>Opens the platform's file picker, filtered by <see cref="Accept"/>.</summary>
    public async Task PickAsync()
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { CanOpen: true } storage) return;
        var picked = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = Multiple,
            FileTypeFilter = FileTypes.Filter(Accept),
        });
        await AddStorageAsync(picked);
    }

    private async Task AddStorageAsync(IEnumerable<IStorageItem> items)
    {
        var entries = new List<FileEntry>();
        foreach (var item in items)
            if (item is IStorageFile file) entries.Add(await FileEntry.FromStorageFileAsync(file));
        if (entries.Count > 0) AddFiles(entries);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || !IsEffectivelyEnabled) return;
        if (e.Key is Key.Enter or Key.Space)
        {
            e.Handled = true;
            _ = PickAsync();
        }
        else if (IsPasteGesture(e))
        {
            e.Handled = true;
            _ = PasteAsync();
        }
    }

    // The platform's own paste keys (Ctrl+V, Cmd+V on macOS); Ctrl+V when the platform has none to tell.
    private static bool IsPasteGesture(KeyEventArgs e) =>
        Application.Current?.PlatformSettings is { } settings
            ? settings.HotkeyConfiguration.Paste.Any(g => g.Matches(e))
            : e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control);

    /// <summary>Adds the clipboard's files, or its image as image.png: the paste gesture's work.</summary>
    public async Task PasteAsync()
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard) return;
        var files = await clipboard.TryGetFilesAsync();
        if (files is { Length: > 0 }) { await AddStorageAsync(files); return; }
        var bitmap = await clipboard.TryGetBitmapAsync();
        if (bitmap is null) return;
        var bytes = new MemoryStream();
        bitmap.Save(bytes, PngBitmapEncoderOptions.Default);
        var data = bytes.ToArray();
        AddFiles([new FileEntry("image.png", data.Length, () => Task.FromResult<Stream>(new MemoryStream(data)), bitmap)]);
    }

    private static bool HasFiles(DragEventArgs e) => e.DataTransfer.Contains(DataFormat.File);

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (!IsEffectivelyEnabled || !HasFiles(e)) { e.DragEffects = DragDropEffects.None; return; }
        _depth++;
        e.DragEffects = DragDropEffects.Copy;
        PseudoClasses.Set(":over", true);
    }

    private void OnDragOver(object? sender, DragEventArgs e) =>
        e.DragEffects = IsEffectivelyEnabled && HasFiles(e) ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (--_depth <= 0) { _depth = 0; PseudoClasses.Set(":over", false); }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        _depth = 0;
        PseudoClasses.Set(":over", false);
        if (!IsEffectivelyEnabled || e.DataTransfer.TryGetFiles() is not { } files) return;
        e.Handled = true;
        _ = AddStorageAsync(files);
    }
}

/// <summary>A row of a <see cref="FileUpload"/>'s list: a picture or the type, the name and size, the upload's state, and the remove button.</summary>
[TemplatePart("PART_Remove", typeof(Button))]
[PseudoClasses(":sending", ":done", ":failed", ":image")]
internal class FileItem : TemplatedControl
{
    public static readonly StyledProperty<string?> SizeTextProperty =
        AvaloniaProperty.Register<FileItem, string?>(nameof(SizeText));

    public static readonly StyledProperty<double> FractionProperty =
        AvaloniaProperty.Register<FileItem, double>(nameof(Fraction));

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<FileItem, bool>(nameof(IsIndeterminate));

    public static readonly StyledProperty<string?> StatusTextProperty =
        AvaloniaProperty.Register<FileItem, string?>(nameof(StatusText));

    public static readonly StyledProperty<string?> RemoveLabelProperty =
        AvaloniaProperty.Register<FileItem, string?>(nameof(RemoveLabel));

    private Button? _remove;
    private readonly bool _enabled;

    static FileItem() => TransformOperationsAnimator.Ensure();

    public FileItem(FileEntry entry, bool enabled = true)
    {
        Entry = entry;
        _enabled = enabled;
        PseudoClasses.Set(":image", entry.Thumbnail is not null);
    }

    public FileEntry Entry { get; }
    public string FileName => Entry.Name;
    public string Extension => Entry.Extension;
    public Bitmap? Thumbnail => Entry.Thumbnail;

    public string? SizeText { get => GetValue(SizeTextProperty); set => SetValue(SizeTextProperty, value); }

    /// <summary>The upload's fraction, 0 to 1.</summary>
    public double Fraction { get => GetValue(FractionProperty); set => SetValue(FractionProperty, value); }

    /// <summary>True while the file is sending and its fraction is not known.</summary>
    public bool IsIndeterminate { get => GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }

    public string? StatusText { get => GetValue(StatusTextProperty); set => SetValue(StatusTextProperty, value); }
    public string? RemoveLabel { get => GetValue(RemoveLabelProperty); set => SetValue(RemoveLabelProperty, value); }

    public event EventHandler? RemoveRequested;

    internal void Refresh(bool arabic)
    {
        SizeText = FileTypes.Size(Entry.Size, arabic);
        RemoveLabel = arabic ? $"إزالة {Entry.Name}" : $"Remove {Entry.Name}";
        Fraction = Entry.Progress ?? 0;
        IsIndeterminate = Entry.Status == FileUploadStatus.Sending && Entry.Progress is null;
        StatusText = Entry.Status == FileUploadStatus.Failed ? (string.IsNullOrEmpty(Entry.Message) ? (arabic ? "تعذر الرفع." : "Upload failed.") : Entry.Message) : null;
        PseudoClasses.Set(":sending", Entry.Status == FileUploadStatus.Sending);
        PseudoClasses.Set(":done", Entry.Status == FileUploadStatus.Done);
        PseudoClasses.Set(":failed", Entry.Status == FileUploadStatus.Failed);
        PseudoClasses.Set(":image", Entry.Thumbnail is not null);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_remove is not null) _remove.Click -= OnRemove;
        base.OnApplyTemplate(e);
        _remove = e.NameScope.Find<Button>("PART_Remove");
        if (_remove is not null)
        {
            _remove.Click += OnRemove;
            _remove.IsEnabled = _enabled;
        }
    }

    private void OnRemove(object? sender, RoutedEventArgs e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
}
