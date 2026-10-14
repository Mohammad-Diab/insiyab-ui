using Avalonia.Logging;

namespace Insiyab.Avalonia.Controls;

// The library's own typing and focus handlers: text they cannot read is logged and shown as invalid, never left to take the app down.
internal static class InputGuard
{
    public static bool Caught(object owner, Exception error)
    {
        Logger.TryGet(LogEventLevel.Warning, "Insiyab")?.Log(owner, "Typed text could not be read: {Error}", error);
        return true;
    }
}
