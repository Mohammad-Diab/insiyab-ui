using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Insiyab.Avalonia.Controls;

/// <summary>Where a file's upload stands, with <see cref="FileUpload.Upload"/> on.</summary>
public enum FileUploadStatus
{
    Ready,
    Sending,
    Done,
    Failed,
}

/// <summary>A file in a <see cref="FileUpload"/>'s list: what was picked, dropped or pasted, and its upload state.</summary>
public sealed class FileEntry
{
    public FileEntry(string name, long size, Func<Task<Stream>>? open = null, Bitmap? thumbnail = null, IStorageFile? file = null)
    {
        Name = name;
        Size = size;
        Open = open;
        Thumbnail = thumbnail;
        File = file;
    }

    public string Name { get; }
    public long Size { get; }

    /// <summary>The picked or dropped file, when it came from the platform.</summary>
    public IStorageFile? File { get; }

    /// <summary>Reads the file's bytes, for the page's upload.</summary>
    public Func<Task<Stream>>? Open { get; }

    /// <summary>A small picture of an image file; null for any other type.</summary>
    public Bitmap? Thumbnail { get; internal set; }

    public FileUploadStatus Status { get; internal set; }

    /// <summary>The upload's fraction, 0 to 1; null while the page has not reported one.</summary>
    public double? Progress { get; internal set; }

    /// <summary>Why the upload failed.</summary>
    public string? Message { get; internal set; }

    /// <summary>The id the page gave a finished upload.</summary>
    public string? UploadId { get; internal set; }

    /// <summary>The extension as the list shows it for a file with no picture: up to four letters, upper-cased.</summary>
    public string Extension
    {
        get
        {
            var dot = Name.LastIndexOf('.');
            if (dot <= 0) return "·";
            var ext = Name[(dot + 1)..];
            return (ext.Length > 4 ? ext[..4] : ext).ToUpperInvariant();
        }
    }

    public static async Task<FileEntry> FromStorageFileAsync(IStorageFile file)
    {
        long size = 0;
        try
        {
            var props = await file.GetBasicPropertiesAsync();
            size = (long)(props.Size ?? 0);
        }
        catch (Exception) { }
        var entry = new FileEntry(file.Name, size, () => file.OpenReadAsync(), null, file);
        if (FileTypes.IsImage(file.Name))
        {
            try
            {
                await using var s = await file.OpenReadAsync();
                entry.Thumbnail = Bitmap.DecodeToWidth(s, 80);
            }
            catch (Exception) { }
        }
        return entry;
    }
}

// What `accept` means for a file that did not come through the picker: extensions, exact types and type/* families.
internal static class FileTypes
{
    private static readonly Dictionary<string, string> Mime = new(StringComparer.OrdinalIgnoreCase)
    {
        ["png"] = "image/png", ["jpg"] = "image/jpeg", ["jpeg"] = "image/jpeg", ["gif"] = "image/gif", ["webp"] = "image/webp",
        ["bmp"] = "image/bmp", ["svg"] = "image/svg+xml", ["avif"] = "image/avif", ["ico"] = "image/x-icon", ["tif"] = "image/tiff",
        ["tiff"] = "image/tiff", ["heic"] = "image/heic", ["pdf"] = "application/pdf", ["txt"] = "text/plain", ["csv"] = "text/csv",
        ["html"] = "text/html", ["json"] = "application/json", ["zip"] = "application/zip", ["mp4"] = "video/mp4", ["webm"] = "video/webm",
        ["mov"] = "video/quicktime", ["mp3"] = "audio/mpeg", ["wav"] = "audio/wav", ["ogg"] = "audio/ogg",
        ["doc"] = "application/msword", ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ["xls"] = "application/vnd.ms-excel", ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ["ppt"] = "application/vnd.ms-powerpoint", ["pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    };

    public static string TypeOf(string name)
    {
        var dot = name.LastIndexOf('.');
        return dot >= 0 && Mime.TryGetValue(name[(dot + 1)..], out var t) ? t : string.Empty;
    }

    public static bool IsImage(string name) => TypeOf(name).StartsWith("image/", StringComparison.Ordinal);

    public static string[] Parse(string? accept) =>
        (accept ?? string.Empty).Split(',').Select(s => s.Trim().ToLowerInvariant()).Where(s => s.Length > 0).ToArray();

    public static bool Accepts(string? accept, string name)
    {
        var list = Parse(accept);
        if (list.Length == 0) return true;
        var lower = name.ToLowerInvariant();
        var type = TypeOf(name);
        foreach (var a in list)
        {
            if (a.StartsWith('.') ? lower.EndsWith(a, StringComparison.Ordinal)
                : a.EndsWith("/*", StringComparison.Ordinal) ? type.StartsWith(a[..^1], StringComparison.Ordinal)
                : type == a)
                return true;
        }
        return false;
    }

    // The picker's filter from `accept`: patterns for extensions and for the families this list knows.
    public static List<FilePickerFileType>? Filter(string? accept)
    {
        var list = Parse(accept);
        if (list.Length == 0) return null;
        var patterns = new List<string>();
        var mimes = new List<string>();
        foreach (var a in list)
        {
            if (a.StartsWith('.')) patterns.Add("*" + a);
            else
            {
                mimes.Add(a);
                foreach (var (ext, type) in Mime)
                    if (a.EndsWith("/*", StringComparison.Ordinal) ? type.StartsWith(a[..^1], StringComparison.Ordinal) : type == a)
                        patterns.Add("*." + ext);
            }
        }
        return [new FilePickerFileType(accept) { Patterns = patterns.Distinct().ToList(), MimeTypes = mimes }];
    }

    public static long Bytes(string? spec)
    {
        if (string.IsNullOrWhiteSpace(spec)) return 0;
        var s = spec.Trim().ToLowerInvariant();
        long unit = 1;
        if (s.EndsWith("gb", StringComparison.Ordinal)) { unit = 1073741824; s = s[..^2]; }
        else if (s.EndsWith("mb", StringComparison.Ordinal)) { unit = 1048576; s = s[..^2]; }
        else if (s.EndsWith("kb", StringComparison.Ordinal)) { unit = 1024; s = s[..^2]; }
        else if (s.EndsWith('b')) s = s[..^1];
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n)
            ? (long)Math.Round(n * unit)
            : 0;
    }

    // A size as the page's language writes it (Intl.NumberFormat's short units), in Latin digits.
    public static string Size(long n, bool arabic)
    {
        var (unit, v) = n >= 1048576 ? (2, n / 1048576d) : n >= 1024 ? (1, n / 1024d) : (0, (double)n);
        var num = PluginText.Num(v, v < 10 ? 1 : 0);
        var name = arabic ? new[] { "بايت", "كيلوبايت", "م.ب" }[unit] : new[] { "byte", "kB", "MB" }[unit];
        return num + " " + name;
    }
}
