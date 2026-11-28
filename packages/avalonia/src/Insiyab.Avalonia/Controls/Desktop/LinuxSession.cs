using System.Runtime.Versioning;
using Tmds.DBus.Protocol;

namespace Insiyab.Avalonia.Controls;

/// <summary>The library's one connection to the Linux session bus for what owns no name there (the desktop portal's settings, the dock's badge). Made on first use off the UI thread and made again after it drops; null where the session has no bus or it does not answer within five seconds. The tray icon keeps its own connection, since it owns a name.</summary>
[SupportedOSPlatform("linux")]
internal static class LinuxSession
{
    internal static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private static readonly Lock s_gate = new();
    private static Task<DBusConnection?>? s_bus;

    /// <summary>The connection, once made; never throws.</summary>
    internal static Task<DBusConnection?> BusAsync()
    {
        lock (s_gate) return s_bus ??= Task.Run(ConnectAsync);
    }

    private static async Task<DBusConnection?> ConnectAsync()
    {
        if (DBusAddress.Session is not { Length: > 0 } address) return null;
        var bus = new DBusConnection(address);
        try
        {
            await bus.ConnectAsync().AsTask().WaitAsync(Limit).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // the bus did not answer (a session still starting): the next use tries again
            bus.Dispose();
            lock (s_gate) s_bus = null;
            return null;
        }
        // dropped: the next use connects again
        _ = bus.DisconnectedAsync().ContinueWith(_ =>
        {
            lock (s_gate) s_bus = null;
        }, TaskScheduler.Default);
        return bus;
    }

    internal delegate void Body(ref MessageWriter writer);

    /// <summary>Calls a method and reads its answer, within <see cref="Limit"/>.</summary>
    internal static Task<T> CallAsync<T>(DBusConnection bus, string destination, string path, string @interface, string member, string? signature, Body? body,
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

    /// <summary>Sends a signal from <paramref name="path"/>; false when it could not be sent.</summary>
    internal static bool Emit(DBusConnection bus, string path, string @interface, string member, string? signature, Body? body)
    {
        try
        {
            var writer = bus.GetMessageWriter();
            try
            {
                writer.WriteSignalHeader(null, path, @interface, member, signature);
                body?.Invoke(ref writer);
                return bus.TrySendMessage(writer.CreateMessage());
            }
            finally
            {
                writer.Dispose();
            }
        }
        catch (Exception)
        {
            return false;
        }
    }
}
