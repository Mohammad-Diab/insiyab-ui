using System.Buffers.Binary;
using System.Runtime.Versioning;
using System.Text;
using Avalonia.Threading;
using Tmds.DBus.Protocol;

namespace Insiyab.Avalonia.Controls;

/// <summary>One property of a menu row as dbusmenu reads it: a string, a bool or an int.</summary>
internal readonly record struct StatusMenuProperty(string Name, object Value);

/// <summary>A row of the menu handed to the desktop: its dbusmenu id (from 1; 0 is the menu itself), the row it came from, and its properties.</summary>
internal sealed record StatusMenuRow(int Id, NotifyAreaMenuItem Source, StatusMenuProperty[] Properties);

/// <summary>One picture of the icon for <c>IconPixmap</c>: its pixels as ARGB, four bytes each in network byte order, row by row from the top.</summary>
internal sealed record StatusPixmap(int Width, int Height, byte[] Argb);

/// <summary>The pure parts of the Linux icon (a StatusNotifierItem with a com.canonical.dbusmenu menu), kept apart so the tests can reach them on any system.</summary>
internal static class StatusNotifier
{
    internal const string WatcherName = "org.kde.StatusNotifierWatcher", WatcherPath = "/StatusNotifierWatcher";
    internal const string ItemInterface = "org.kde.StatusNotifierItem", ItemPath = "/StatusNotifierItem";
    internal const string MenuInterface = "com.canonical.dbusmenu", MenuPath = "/MenuBar";

    /// <summary>The properties of <c>org.kde.StatusNotifierItem</c> the icon answers, in the order <c>GetAll</c> gives them.</summary>
    internal static readonly string[] ItemProperties =
    [
        "Category", "Id", "Title", "Status", "WindowId", "IconThemePath", "IconName", "IconPixmap", "OverlayIconName", "OverlayIconPixmap",
        "AttentionIconName", "AttentionIconPixmap", "AttentionMovieName", "ToolTip", "ItemIsMenu", "Menu",
    ];

    /// <summary>The properties of the menu itself.</summary>
    internal static readonly string[] MenuProperties = ["Version", "TextDirection", "Status", "IconThemePath"];

    /// <summary>The menu's root: its rows open as a submenu.</summary>
    internal static readonly StatusMenuProperty[] RootProperties = [new("children-display", "submenu")];

    /// <summary>The bus name an icon asks for: one per process and icon, as the specification asks.</summary>
    internal static string BusName(int process, int icon) => $"org.kde.StatusNotifierItem-{process}-{icon}";

    /// <summary>A row's text as dbusmenu reads it: an underscore marks the access key there, so a written one is doubled.</summary>
    internal static string Label(string text) => text.Replace("_", "__", StringComparison.Ordinal);

    /// <summary>The menu's <c>TextDirection</c>.</summary>
    internal static string Direction(bool rightToLeft) => rightToLeft ? "rtl" : "ltr";

    /// <summary>A row's properties, only those that differ from dbusmenu's defaults (a standard, enabled, visible row with no toggle).</summary>
    internal static StatusMenuProperty[] Properties(NotifyAreaMenuItem row)
    {
        if (row.IsSeparator) return [new("type", "separator")];
        var list = new List<StatusMenuProperty> { new("label", Label(row.Text)) };
        if (!row.IsEnabled) list.Add(new("enabled", false));
        if (row.IsChecked)
        {
            list.Add(new("toggle-type", "checkmark"));
            list.Add(new("toggle-state", 1));
        }
        return [.. list];
    }

    /// <summary>The rows the desktop gets for <paramref name="rows"/>, numbered from 1.</summary>
    internal static StatusMenuRow[] Rows(IReadOnlyList<NotifyAreaMenuItem> rows)
    {
        var made = new StatusMenuRow[rows.Count];
        for (var i = 0; i < rows.Count; i++) made[i] = new StatusMenuRow(i + 1, rows[i], Properties(rows[i]));
        return made;
    }

    /// <summary>Whether two sets of rows read the same to the desktop (the same properties in the same order), so no new revision is needed.</summary>
    internal static bool Same(StatusMenuRow[] a, StatusMenuRow[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (!a[i].Properties.AsSpan().SequenceEqual(b[i].Properties)) return false;
        return true;
    }

    /// <summary>The icon's pictures for <c>IconPixmap</c>: every 32-bit picture of the .ico, one per size, smallest first; empty when it has none (a PNG picture is left out).</summary>
    internal static IReadOnlyList<StatusPixmap> Pixmaps(byte[] ico)
    {
        var found = new SortedDictionary<int, StatusPixmap>();
        if (ico.Length < 6 || BitConverter.ToUInt16(ico, 0) != 0 || BitConverter.ToUInt16(ico, 2) != 1) return [];
        var count = BitConverter.ToUInt16(ico, 4);
        for (var i = 0; i < count && 6 + 16 * (i + 1) <= ico.Length; i++)
        {
            var entry = 6 + 16 * i;
            var bytes = BitConverter.ToInt32(ico, entry + 8);
            var offset = BitConverter.ToInt32(ico, entry + 12);
            if (offset < 0 || bytes <= 0 || (long)offset + bytes > ico.Length) continue;
            if (FromDib(ico.AsSpan(offset, bytes)) is { } pixmap) found.TryAdd(pixmap.Width, pixmap);
        }
        return [.. found.Values];
    }

    /// <summary>A 32-bit picture of an .ico (a BITMAPINFOHEADER, the colour rows from the bottom up as BGRA, then the 1-bit mask) as ARGB in network byte order; null for any other kind. A picture whose alpha is all zero takes its transparency from the mask.</summary>
    internal static StatusPixmap? FromDib(ReadOnlySpan<byte> dib)
    {
        if (dib.Length < 40) return null;
        var header = BinaryPrimitives.ReadInt32LittleEndian(dib);
        var width = BinaryPrimitives.ReadInt32LittleEndian(dib[4..]);
        var height = BinaryPrimitives.ReadInt32LittleEndian(dib[8..]) / 2;
        var bits = BinaryPrimitives.ReadUInt16LittleEndian(dib[14..]);
        var compression = BinaryPrimitives.ReadUInt32LittleEndian(dib[16..]);
        if (header < 40 || header > dib.Length || bits != 32 || compression != 0 || width <= 0 || height <= 0 || width > 1024 || height > 1024) return null;
        var stride = width * 4;
        var maskStride = (width + 31) / 32 * 4;
        if ((long)header + (long)stride * height > dib.Length) return null;
        var pixels = dib.Slice(header, stride * height);
        var hasMask = (long)header + (long)stride * height + (long)maskStride * height <= dib.Length;
        var mask = hasMask ? dib.Slice(header + stride * height, maskStride * height) : default;
        var anyAlpha = false;
        for (var i = 3; i < pixels.Length; i += 4)
            if (pixels[i] != 0)
            {
                anyAlpha = true;
                break;
            }
        var argb = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var from = (height - 1 - y) * stride; // the picture's rows are kept from the bottom up
            for (var x = 0; x < width; x++)
            {
                var p = from + x * 4;
                byte alpha;
                if (anyAlpha) alpha = pixels[p + 3];
                else if (hasMask) alpha = (mask[(height - 1 - y) * maskStride + x / 8] & (0x80 >> (x % 8))) != 0 ? (byte)0 : (byte)255;
                else alpha = 255;
                var to = (y * width + x) * 4;
                argb[to] = alpha;
                argb[to + 1] = pixels[p + 2];
                argb[to + 2] = pixels[p + 1];
                argb[to + 3] = pixels[p];
            }
        }
        return new StatusPixmap(width, height, argb);
    }

    // the introspection data, for tools and desktops that ask (the hosts use their own copies of these interfaces)
    internal static readonly ReadOnlyMemory<byte> ItemXml = Encoding.UTF8.GetBytes("""
        <interface name="org.kde.StatusNotifierItem">
          <property name="Category" type="s" access="read"/><property name="Id" type="s" access="read"/><property name="Title" type="s" access="read"/>
          <property name="Status" type="s" access="read"/><property name="WindowId" type="i" access="read"/><property name="IconThemePath" type="s" access="read"/>
          <property name="IconName" type="s" access="read"/><property name="IconPixmap" type="a(iiay)" access="read"/>
          <property name="OverlayIconName" type="s" access="read"/><property name="OverlayIconPixmap" type="a(iiay)" access="read"/>
          <property name="AttentionIconName" type="s" access="read"/><property name="AttentionIconPixmap" type="a(iiay)" access="read"/>
          <property name="AttentionMovieName" type="s" access="read"/><property name="ToolTip" type="(sa(iiay)ss)" access="read"/>
          <property name="ItemIsMenu" type="b" access="read"/><property name="Menu" type="o" access="read"/>
          <method name="ContextMenu"><arg name="x" type="i" direction="in"/><arg name="y" type="i" direction="in"/></method>
          <method name="Activate"><arg name="x" type="i" direction="in"/><arg name="y" type="i" direction="in"/></method>
          <method name="SecondaryActivate"><arg name="x" type="i" direction="in"/><arg name="y" type="i" direction="in"/></method>
          <method name="Scroll"><arg name="delta" type="i" direction="in"/><arg name="orientation" type="s" direction="in"/></method>
          <method name="ProvideXdgActivationToken"><arg name="token" type="s" direction="in"/></method>
          <signal name="NewTitle"/><signal name="NewIcon"/><signal name="NewAttentionIcon"/><signal name="NewOverlayIcon"/><signal name="NewToolTip"/>
          <signal name="NewStatus"><arg name="status" type="s"/></signal>
        </interface>
        """);

    internal static readonly ReadOnlyMemory<byte> MenuXml = Encoding.UTF8.GetBytes("""
        <interface name="com.canonical.dbusmenu">
          <property name="Version" type="u" access="read"/><property name="TextDirection" type="s" access="read"/>
          <property name="Status" type="s" access="read"/><property name="IconThemePath" type="as" access="read"/>
          <method name="GetLayout"><arg type="i" name="parentId" direction="in"/><arg type="i" name="recursionDepth" direction="in"/>
            <arg type="as" name="propertyNames" direction="in"/><arg type="u" name="revision" direction="out"/><arg type="(ia{sv}av)" name="layout" direction="out"/></method>
          <method name="GetGroupProperties"><arg type="ai" name="ids" direction="in"/><arg type="as" name="propertyNames" direction="in"/>
            <arg type="a(ia{sv})" name="properties" direction="out"/></method>
          <method name="GetProperty"><arg type="i" name="id" direction="in"/><arg type="s" name="name" direction="in"/><arg type="v" name="value" direction="out"/></method>
          <method name="Event"><arg type="i" name="id" direction="in"/><arg type="s" name="eventId" direction="in"/><arg type="v" name="data" direction="in"/>
            <arg type="u" name="timestamp" direction="in"/></method>
          <method name="EventGroup"><arg type="a(isvu)" name="events" direction="in"/><arg type="ai" name="idErrors" direction="out"/></method>
          <method name="AboutToShow"><arg type="i" name="id" direction="in"/><arg type="b" name="needUpdate" direction="out"/></method>
          <method name="AboutToShowGroup"><arg type="ai" name="ids" direction="in"/><arg type="ai" name="updatesNeeded" direction="out"/>
            <arg type="ai" name="idErrors" direction="out"/></method>
          <signal name="ItemsPropertiesUpdated"><arg type="a(ia{sv})" name="updatedProps"/><arg type="a(ias)" name="removedProps"/></signal>
          <signal name="LayoutUpdated"><arg type="u" name="revision"/><arg type="i" name="parent"/></signal>
          <signal name="ItemActivationRequested"><arg type="i" name="id"/><arg type="u" name="timestamp"/></signal>
        </interface>
        """);
}

/// <summary>One icon on a Linux desktop: a StatusNotifierItem on the session bus (its own connection), registered with the desktop's StatusNotifierWatcher, with its menu as com.canonical.dbusmenu. Shown once the watcher is there with a host, which may be later (a panel still starting, GNOME's AppIndicator extension turned on), and registered again when the watcher comes back after a restart. The desktop calls in on the connection's own thread; every event goes to the UI thread. Nothing here throws or waits on the UI thread: a desktop without a session bus or a watcher simply never shows the icon.</summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxNotifyArea : INotifyAreaShell
{
    private const string Bus = "org.freedesktop.DBus", BusPath = "/org/freedesktop/DBus";
    private const string Properties = "org.freedesktop.DBus.Properties";
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);
    private static int s_count;
    private static volatile bool s_hostSeen;

    private readonly Lock _gate = new();
    private readonly SemaphoreSlim _checking = new(1, 1);
    private readonly IReadOnlyList<StatusPixmap> _pixmaps;
    private readonly string _id;
    private readonly string _wanted;
    private string _itemName;
    private string _tip = "";
    private StatusMenuRow[] _rows = [];
    private uint _revision = 1;
    private bool _rightToLeft;
    private DBusConnection? _bus;
    private string? _registeredWith; // the watcher's unique name the item was registered with
    private bool _started, _shown, _disposed;

    private LinuxNotifyArea(IReadOnlyList<StatusPixmap> pixmaps, string id)
    {
        _pixmaps = pixmaps;
        _id = id;
        _wanted = _itemName = StatusNotifier.BusName(Environment.ProcessId, Interlocked.Increment(ref s_count));
    }

    /// <summary>The last answer any icon got: the desktop's watcher is there with a host.</summary>
    internal static bool HostSeen => s_hostSeen;

    public Action<NotifyAreaEvent, int, int>? Raised { get; set; }

    /// <summary>The icon's side on this desktop, or null where the session has no bus.</summary>
    internal static LinuxNotifyArea? TryMake(byte[] ico)
    {
        if (!OperatingSystem.IsLinux() || DBusAddress.Session is not { Length: > 0 }) return null;
        var id = Path.GetFileNameWithoutExtension(Environment.ProcessPath) is { Length: > 0 } name ? name : "app";
        return new LinuxNotifyArea(StatusNotifier.Pixmaps(ico), id);
    }

    public bool Add(string tip)
    {
        lock (_gate)
        {
            _tip = tip;
            if (_started || _disposed) return _shown;
            _started = true;
        }
        Task.Run(StartAsync);
        return false;
    }

    public void SetTip(string tip)
    {
        lock (_gate) _tip = tip;
        Emit(StatusNotifier.ItemPath, StatusNotifier.ItemInterface, "NewToolTip");
        Emit(StatusNotifier.ItemPath, StatusNotifier.ItemInterface, "NewTitle");
    }

    // the desktop's own notifications are the app's on Linux
    public void ShowBalloon(string title, string text, NotifyAreaLevel level)
    {
    }

    public int ShowMenu(IReadOnlyList<NotifyAreaMenuItem> rows, int x, int y, bool rightToLeft)
    {
        var made = StatusNotifier.Rows(rows);
        bool changed, turned;
        uint revision;
        lock (_gate)
        {
            changed = !StatusNotifier.Same(_rows, made);
            turned = _rightToLeft != rightToLeft;
            _rows = made; // the new rows' clicks, even when they read the same
            _rightToLeft = rightToLeft;
            if (changed || turned) _revision++;
            revision = _revision;
        }
        if (turned)
            Emit(StatusNotifier.MenuPath, Properties, "PropertiesChanged", "sa{sv}as", (ref MessageWriter w) =>
            {
                w.WriteString(StatusNotifier.MenuInterface);
                var d = w.WriteDictionaryStart();
                w.WriteDictionaryEntryStart();
                w.WriteString("TextDirection");
                w.WriteVariantString(StatusNotifier.Direction(rightToLeft));
                w.WriteDictionaryEnd(d);
                w.WriteArray(Array.Empty<string>());
            });
        if (changed || turned)
            Emit(StatusNotifier.MenuPath, StatusNotifier.MenuInterface, "LayoutUpdated", "ui", (ref MessageWriter w) =>
            {
                w.WriteUInt32(revision);
                w.WriteInt32(0);
            });
        return 0;
    }

    public void Dispose()
    {
        DBusConnection? bus;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            bus = _bus;
            _bus = null;
        }
        Raised = null;
        try
        {
            bus?.Dispose(); // the bus lets the name go, and the watcher drops the item
        }
        catch (Exception)
        {
        }
    }

    private async Task StartAsync()
    {
        try
        {
            var bus = new DBusConnection(DBusAddress.Session!);
            try
            {
                await bus.ConnectAsync().AsTask().WaitAsync(Limit).ConfigureAwait(false);
            }
            catch (Exception)
            {
                bus.Dispose();
                return;
            }
            lock (_gate)
            {
                if (_disposed)
                {
                    bus.Dispose();
                    return;
                }
                _bus = bus;
            }
            bus.AddMethodHandlers([new Handler(this, StatusNotifier.ItemPath), new Handler(this, StatusNotifier.MenuPath)]);
            if (!await bus.TryRequestNameAsync(_wanted, RequestNameOptions.None).WaitAsync(Limit).ConfigureAwait(false))
                _itemName = bus.UniqueName ?? _wanted;
            // the rows written before the desktop first asks for them
            Post(NotifyAreaEvent.Menu, 0, 0);
            _ = bus.DisconnectedAsync().ContinueWith(_ => SetShown(false), TaskScheduler.Default);
            // the watcher came, went or was replaced (a panel restarting, GNOME's extension turned on or off), or a host came or went
            await bus.AddMatchAsync(
                new MatchRule { Type = MessageType.Signal, Sender = Bus, Path = BusPath, Interface = Bus, Member = "NameOwnerChanged", Arg0 = StatusNotifier.WatcherName },
                static (_, _) => true, (Notification<bool> n) => { if (n.HasValue) Recheck(); }, false, ObserverFlags.None, null)
                .AsTask().WaitAsync(Limit).ConfigureAwait(false);
            foreach (var signal in (string[])["StatusNotifierHostRegistered", "StatusNotifierHostUnregistered"])
                await bus.WatchSignalAsync(StatusNotifier.WatcherName, StatusNotifier.WatcherPath, StatusNotifier.WatcherName, signal,
                    (Tmds.DBus.Protocol.Notification n) => { if (n.Type == NotificationType.Value) Recheck(); }, ObserverFlags.None, false, null)
                    .AsTask().WaitAsync(Limit).ConfigureAwait(false);
            await RecheckAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            SetShown(false);
        }
    }

    private void Recheck() => Task.Run(RecheckAsync);

    // whether the watcher is there with a host; registers the item with a watcher it was not registered with yet
    private async Task RecheckAsync()
    {
        await _checking.WaitAsync().ConfigureAwait(false);
        try
        {
            DBusConnection? bus;
            lock (_gate) bus = _disposed ? null : _bus;
            if (bus is null) return;
            var shown = false;
            var owner = await OwnerAsync(bus, StatusNotifier.WatcherName).ConfigureAwait(false);
            if (owner is null) _registeredWith = null;
            else if (await HostAsync(bus).ConfigureAwait(false))
            {
                if (_registeredWith != owner)
                {
                    var name = _itemName;
                    await CallAsync(bus, StatusNotifier.WatcherName, StatusNotifier.WatcherPath, StatusNotifier.WatcherName, "RegisterStatusNotifierItem", "s",
                        (ref MessageWriter w) => w.WriteString(name), static (_, _) => true).ConfigureAwait(false);
                    _registeredWith = owner;
                }
                shown = true;
            }
            s_hostSeen = shown;
            SetShown(shown);
        }
        catch (Exception)
        {
            _registeredWith = null;
            SetShown(false);
        }
        finally
        {
            _checking.Release();
        }
    }

    private static async Task<string?> OwnerAsync(DBusConnection bus, string name)
    {
        try
        {
            return await CallAsync(bus, Bus, BusPath, Bus, "GetNameOwner", "s", (ref MessageWriter w) => w.WriteString(name),
                static (m, _) => m.GetBodyReader().ReadString()).ConfigureAwait(false);
        }
        catch (DBusErrorReplyException)
        {
            return null; // nobody owns it
        }
    }

    // a watcher that cannot say counts as having a host: it is there, and the icon is offered to it
    private static async Task<bool> HostAsync(DBusConnection bus)
    {
        try
        {
            return await CallAsync(bus, StatusNotifier.WatcherName, StatusNotifier.WatcherPath, Properties, "Get", "ss",
                (ref MessageWriter w) =>
                {
                    w.WriteString(StatusNotifier.WatcherName);
                    w.WriteString("IsStatusNotifierHostRegistered");
                },
                static (m, _) => m.GetBodyReader().ReadVariantValue() is { Type: VariantValueType.Bool } v ? v.GetBool() : true).ConfigureAwait(false);
        }
        catch (DBusErrorReplyException)
        {
            return true;
        }
    }

    private void SetShown(bool shown)
    {
        bool changed;
        lock (_gate)
        {
            changed = !_disposed && _shown != shown;
            _shown = shown;
        }
        if (changed) Post(NotifyAreaEvent.Added, shown ? 1 : 0, 0);
    }

    private void Post(NotifyAreaEvent kind, int x, int y) => Dispatcher.UIThread.Post(() =>
    {
        if (!_disposed) Raised?.Invoke(kind, x, y);
    });

    internal delegate void Body(ref MessageWriter writer);

    private static Task<T> CallAsync<T>(DBusConnection bus, string destination, string path, string @interface, string member, string? signature, Body? body,
        MessageValueReader<T> reader)
    {
        var writer = bus.GetMessageWriter();
        MessageBuffer message;
        try
        {
            writer.WriteMethodCallHeader(destination: destination, path: path, @interface: @interface, member: member, signature: signature);
            body?.Invoke(ref writer);
            message = writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }
        return bus.CallMethodAsync(message, reader, null).WaitAsync(Limit);
    }

    private void Emit(string path, string @interface, string member, string? signature = null, Body? body = null)
    {
        DBusConnection? bus;
        lock (_gate) bus = _disposed || !_started ? null : _bus;
        if (bus is null) return;
        try
        {
            var writer = bus.GetMessageWriter();
            try
            {
                writer.WriteSignalHeader(null, path, @interface, member, signature);
                body?.Invoke(ref writer);
                bus.TrySendMessage(writer.CreateMessage());
            }
            finally
            {
                writer.Dispose();
            }
        }
        catch (Exception)
        {
        }
    }

    private static void Reply(MethodContext context, string? signature, Body? body)
    {
        var writer = context.CreateReplyWriter(signature);
        try
        {
            body?.Invoke(ref writer);
            context.Reply(writer.CreateMessage());
        }
        finally
        {
            writer.Dispose();
        }
    }

    // ---- the desktop calling in (on the connection's thread) ----

    private sealed class Handler(LinuxNotifyArea icon, string path) : IPathMethodHandler
    {
        public string Path => path;

        public bool HandlesChildPaths => false;

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            try
            {
                if (path == StatusNotifier.MenuPath) icon.OnMenuCall(context);
                else icon.OnItemCall(context);
            }
            catch (Exception)
            {
                if (!context.ReplySent && !context.DisposesAsynchronously && !context.NoReplyExpected) context.ReplyError("org.freedesktop.DBus.Error.Failed", "failed");
            }
            return default;
        }
    }

    private (string Tip, StatusMenuRow[] Rows, uint Revision, bool RightToLeft) Snapshot()
    {
        lock (_gate) return (_tip, _rows, _revision, _rightToLeft);
    }

    private void OnItemCall(MethodContext context)
    {
        var request = context.Request;
        if (context.IsDBusIntrospectRequest)
        {
            context.ReplyIntrospectXml([StatusNotifier.ItemXml, IntrospectionXml.DBusProperties, IntrospectionXml.DBusIntrospectable], (IList<string>)Array.Empty<string>());
            return;
        }
        if (context.IsPropertiesInterfaceRequest)
        {
            OnProperties(context, StatusNotifier.ItemInterface, StatusNotifier.ItemProperties, WriteItemProperty);
            return;
        }
        switch (request.MemberAsString)
        {
            case "Activate":
                Post(NotifyAreaEvent.Activated, 0, 0);
                break;
            case "ContextMenu" or "SecondaryActivate" or "Scroll" or "ProvideXdgActivationToken":
                break;
            default:
                context.ReplyUnknownMethodError();
                return;
        }
        if (!context.NoReplyExpected) Reply(context, null, null);
    }

    private void OnProperties(MethodContext context, string @interface, string[] names, PropertyWriter write)
    {
        var reader = context.Request.GetBodyReader();
        switch (context.Request.MemberAsString)
        {
            case "Get":
                var asked = reader.ReadString();
                var name = reader.ReadString();
                if (asked != @interface || Array.IndexOf(names, name) < 0)
                {
                    context.ReplyError("org.freedesktop.DBus.Error.UnknownProperty", name);
                    return;
                }
                Reply(context, "v", (ref MessageWriter w) => write(ref w, name));
                break;
            case "GetAll":
                var all = reader.ReadString() == @interface;
                Reply(context, "a{sv}", (ref MessageWriter w) =>
                {
                    var d = w.WriteDictionaryStart();
                    if (all)
                        foreach (var each in names)
                        {
                            w.WriteDictionaryEntryStart();
                            w.WriteString(each);
                            write(ref w, each);
                        }
                    w.WriteDictionaryEnd(d);
                });
                break;
            case "Set":
                context.ReplyError("org.freedesktop.DBus.Error.PropertyReadOnly", "read only");
                break;
            default:
                context.ReplyUnknownMethodError();
                break;
        }
    }

    private delegate void PropertyWriter(ref MessageWriter writer, string name);

    private void WriteItemProperty(ref MessageWriter w, string name)
    {
        var tip = Snapshot().Tip;
        switch (name)
        {
            case "Category":
                w.WriteVariantString("ApplicationStatus");
                break;
            case "Id":
                w.WriteVariantString(_id);
                break;
            case "Title":
                w.WriteVariantString(tip);
                break;
            case "Status":
                w.WriteVariantString("Active");
                break;
            case "WindowId":
                w.WriteVariantInt32(0);
                break;
            case "IconPixmap":
                w.WriteSignature("a(iiay)");
                WritePixmaps(ref w, _pixmaps);
                break;
            case "OverlayIconPixmap" or "AttentionIconPixmap":
                w.WriteSignature("a(iiay)");
                WritePixmaps(ref w, []);
                break;
            case "ToolTip":
                w.WriteSignature("(sa(iiay)ss)");
                w.WriteStructureStart();
                w.WriteString("");
                WritePixmaps(ref w, []);
                w.WriteString(tip);
                w.WriteString("");
                break;
            case "ItemIsMenu":
                w.WriteVariantBool(false);
                break;
            case "Menu":
                w.WriteVariantObjectPath(StatusNotifier.MenuPath);
                break;
            default: // the names, the theme path and the movie: none
                w.WriteVariantString("");
                break;
        }
    }

    private static void WritePixmaps(ref MessageWriter w, IReadOnlyList<StatusPixmap> pixmaps)
    {
        var array = w.WriteArrayStart(DBusType.Struct);
        foreach (var p in pixmaps)
        {
            w.WriteStructureStart();
            w.WriteInt32(p.Width);
            w.WriteInt32(p.Height);
            w.WriteArray(p.Argb);
        }
        w.WriteArrayEnd(array);
    }

    private void WriteMenuProperty(ref MessageWriter w, string name)
    {
        switch (name)
        {
            case "Version":
                w.WriteVariantUInt32(3);
                break;
            case "TextDirection":
                w.WriteVariantString(StatusNotifier.Direction(Snapshot().RightToLeft));
                break;
            case "Status":
                w.WriteVariantString("normal");
                break;
            default:
                w.WriteSignature("as");
                w.WriteArray(Array.Empty<string>());
                break;
        }
    }

    private void OnMenuCall(MethodContext context)
    {
        if (context.IsDBusIntrospectRequest)
        {
            context.ReplyIntrospectXml([StatusNotifier.MenuXml, IntrospectionXml.DBusProperties, IntrospectionXml.DBusIntrospectable], (IList<string>)Array.Empty<string>());
            return;
        }
        if (context.IsPropertiesInterfaceRequest)
        {
            OnProperties(context, StatusNotifier.MenuInterface, StatusNotifier.MenuProperties, WriteMenuProperty);
            return;
        }
        var reader = context.Request.GetBodyReader();
        var (_, rows, revision, _) = Snapshot();
        switch (context.Request.MemberAsString)
        {
            case "GetLayout":
            {
                var parent = reader.ReadInt32();
                var depth = reader.ReadInt32();
                var row = parent == 0 ? null : Array.Find(rows, r => r.Id == parent);
                if (parent != 0 && row is null)
                {
                    context.ReplyError("org.freedesktop.DBus.Error.InvalidArgs", "no such row");
                    return;
                }
                Reply(context, "u(ia{sv}av)", (ref MessageWriter w) =>
                {
                    w.WriteUInt32(revision);
                    if (row is null) WriteNode(ref w, 0, StatusNotifier.RootProperties, depth == 0 ? Array.Empty<StatusMenuRow>() : rows);
                    else WriteNode(ref w, row.Id, row.Properties, []);
                });
                return;
            }
            case "GetGroupProperties":
            {
                var ids = reader.ReadArrayOfInt32();
                Reply(context, "a(ia{sv})", (ref MessageWriter w) =>
                {
                    var array = w.WriteArrayStart(DBusType.Struct);
                    foreach (var row in rows)
                        if (ids.Length == 0 || Array.IndexOf(ids, row.Id) >= 0)
                        {
                            w.WriteStructureStart();
                            w.WriteInt32(row.Id);
                            WriteProperties(ref w, row.Properties);
                        }
                    if (Array.IndexOf(ids, 0) >= 0)
                    {
                        w.WriteStructureStart();
                        w.WriteInt32(0);
                        WriteProperties(ref w, StatusNotifier.RootProperties);
                    }
                    w.WriteArrayEnd(array);
                });
                return;
            }
            case "GetProperty":
            {
                var id = reader.ReadInt32();
                var name = reader.ReadString();
                var properties = id == 0 ? StatusNotifier.RootProperties : Array.Find(rows, r => r.Id == id)?.Properties;
                var at = properties is null ? -1 : Array.FindIndex(properties, p => p.Name == name);
                if (at < 0)
                {
                    context.ReplyError("org.freedesktop.DBus.Error.InvalidArgs", "no such property");
                    return;
                }
                var value = properties![at].Value;
                Reply(context, "v", (ref MessageWriter w) => WriteValue(ref w, value));
                return;
            }
            case "Event":
            {
                var id = reader.ReadInt32();
                var kind = reader.ReadString();
                if (kind == "clicked") Click(rows, id);
                break;
            }
            case "EventGroup":
            {
                var missing = new List<int>();
                var end = reader.ReadArrayStart(DBusType.Struct);
                while (reader.HasNext(end))
                {
                    reader.AlignStruct();
                    var id = reader.ReadInt32();
                    var kind = reader.ReadString();
                    reader.ReadVariantValue();
                    reader.ReadUInt32();
                    if (id != 0 && Array.Find(rows, r => r.Id == id) is null) missing.Add(id);
                    else if (kind == "clicked") Click(rows, id);
                }
                Reply(context, "ai", (ref MessageWriter w) => w.WriteArray(missing.ToArray()));
                return;
            }
            case "AboutToShow":
                if (reader.ReadInt32() == 0) Refresh(context, group: false);
                else Reply(context, "b", (ref MessageWriter w) => w.WriteBool(false));
                return;
            case "AboutToShowGroup":
                if (Array.IndexOf(reader.ReadArrayOfInt32(), 0) >= 0) Refresh(context, group: true);
                else Reply(context, "aiai", (ref MessageWriter w) =>
                {
                    w.WriteArray(Array.Empty<int>());
                    w.WriteArray(Array.Empty<int>());
                });
                return;
            default:
                context.ReplyUnknownMethodError();
                return;
        }
        if (!context.NoReplyExpected) Reply(context, null, null);
    }

    // the menu is about to open: the app writes its rows again on the UI thread (a tick, a greyed row, a new language), then the desktop hears whether they changed
    private void Refresh(MethodContext context, bool group)
    {
        context.DisposesAsynchronously = true;
        Dispatcher.UIThread.Post(() =>
        {
            var changed = false;
            try
            {
                var before = Snapshot().Revision;
                if (!_disposed) Raised?.Invoke(NotifyAreaEvent.Menu, 0, 0);
                changed = Snapshot().Revision != before;
            }
            catch (Exception)
            {
            }
            try
            {
                if (group)
                    Reply(context, "aiai", (ref MessageWriter w) =>
                    {
                        w.WriteArray(changed ? new[] { 0 } : Array.Empty<int>());
                        w.WriteArray(Array.Empty<int>());
                    });
                else Reply(context, "b", (ref MessageWriter w) => w.WriteBool(changed));
            }
            catch (Exception)
            {
            }
            finally
            {
                context.Dispose();
            }
        });
    }

    // a row picked in the desktop's menu runs on the UI thread, if it can still be picked
    private void Click(StatusMenuRow[] rows, int id)
    {
        if (Array.Find(rows, r => r.Id == id) is not { } row) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (!_disposed && row.Source is { IsSeparator: false, IsEnabled: true }) row.Source.RaiseClick();
        });
    }

    private static void WriteNode(ref MessageWriter w, int id, StatusMenuProperty[] properties, StatusMenuRow[] children)
    {
        w.WriteStructureStart();
        w.WriteInt32(id);
        WriteProperties(ref w, properties);
        var array = w.WriteArrayStart(DBusType.Variant);
        foreach (var child in children)
        {
            w.WriteSignature("(ia{sv}av)");
            WriteNode(ref w, child.Id, child.Properties, []);
        }
        w.WriteArrayEnd(array);
    }

    private static void WriteProperties(ref MessageWriter w, StatusMenuProperty[] properties)
    {
        var d = w.WriteDictionaryStart();
        foreach (var p in properties)
        {
            w.WriteDictionaryEntryStart();
            w.WriteString(p.Name);
            WriteValue(ref w, p.Value);
        }
        w.WriteDictionaryEnd(d);
    }

    private static void WriteValue(ref MessageWriter w, object value)
    {
        switch (value)
        {
            case bool b:
                w.WriteVariantBool(b);
                break;
            case int i:
                w.WriteVariantInt32(i);
                break;
            default:
                w.WriteVariantString(value as string ?? "");
                break;
        }
    }
}
