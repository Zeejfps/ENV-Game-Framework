using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using ZGF.Geometry;

namespace ZGF.Gui.Web.Files;

/// <summary>
/// [JSImport] surface over <c>files.js</c> plus metadata parsing shared by the
/// picker and drag-drop paths.
/// </summary>
[SupportedOSPlatform("browser")]
internal static partial class WebFiles
{
    public static async Task InitAsync() => await JSHost.ImportAsync("files", "./files.js");

    // openPicker calls input.click() synchronously, so the JSImport call must happen
    // inside a live user gesture; the returned task resolves when the user chooses.
    [JSImport("openPicker", "files")] public static partial Task<string> OpenPicker(bool multiple, string accept);
    [JSImport("loadFile", "files")] public static partial Task<int> LoadFile(int handle);
    [JSImport("readInto", "files")] public static partial void ReadInto(int handle, [JSMarshalAs<JSType.MemoryView>] Span<byte> dest);
    [JSImport("freeFile", "files")] public static partial void FreeFile(int handle);

    public static IReadOnlyList<PickedFile> Parse(string metaJson)
    {
        var list = new List<PickedFile>();
        if (string.IsNullOrEmpty(metaJson)) return list;
        using var doc = JsonDocument.Parse(metaJson);
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            list.Add(new PickedFile(
                el.GetProperty("h").GetInt32(),
                el.GetProperty("name").GetString() ?? "",
                el.GetProperty("size").GetInt64(),
                el.GetProperty("type").GetString() ?? ""));
        }
        return list;
    }
}

/// <summary>Web <see cref="IFilePicker"/> over a hidden <c>&lt;input type=file&gt;</c>.</summary>
[SupportedOSPlatform("browser")]
public sealed class WebFilePicker : IFilePicker
{
    public static Task InitAsync() => WebFiles.InitAsync();

    public Task<IReadOnlyList<PickedFile>> PickFilesAsync(FilePickOptions options)
    {
        // IMPORTANT: do not await before WebFiles.OpenPicker — that call triggers
        // input.click() synchronously and depends on the caller still being inside
        // the user gesture (i.e. PickFilesAsync must be reached synchronously from a
        // click/pointer handler). The await below only suspends for the user's choice.
        return PickAsync(options);
    }

    private static async Task<IReadOnlyList<PickedFile>> PickAsync(FilePickOptions o)
    {
        var json = await WebFiles.OpenPicker(o.Multiple, o.Accept);
        return WebFiles.Parse(json);
    }
}

/// <summary>
/// Drag-and-drop of files onto the canvas. <c>main.js</c> owns the DOM drag
/// listeners (it has the assembly exports) and calls these [JSExport] hooks; drop
/// coordinates arrive already in canvas-logical, Y-up GUI space. Contents are only
/// available on drop (the browser withholds them during dragover).
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class WebFileDrop
{
    public static bool IsDragOver { get; private set; }
    public static PointF DragPoint { get; private set; } = new(float.MinValue, float.MinValue);

    /// Fires on the UI message loop with the dropped files.
    public static event Action<PointF, IReadOnlyList<PickedFile>>? FilesDropped;

    [JSExport]
    internal static void DragOver(double x, double y)
    {
        IsDragOver = true;
        DragPoint = new PointF((float)x, (float)y);
    }

    [JSExport]
    internal static void DragLeave() => IsDragOver = false;

    [JSExport]
    internal static void Drop(double x, double y, string metaJson)
    {
        IsDragOver = false;
        var point = new PointF((float)x, (float)y);
        DragPoint = point;
        FilesDropped?.Invoke(point, WebFiles.Parse(metaJson));
    }
}
