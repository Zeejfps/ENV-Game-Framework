using System.Reflection;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy.App.Tests;

/// <summary>The app state never reaches the screen over it, through data or callbacks.</summary>
public sealed class ScreenIsolationTests : IDisposable
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly Assembly[] WalkedAssemblies =
    [
        typeof(AppState).Assembly, typeof(LoadedAssembly).Assembly, typeof(DockLayout).Assembly, typeof(Ui).Assembly, typeof(object).Assembly,
    ];

    private static readonly Type[] ScreenTypes =
    [
        typeof(UiState), typeof(TabView), typeof(PictureView), typeof(BrowseTree), typeof(AnalyzerRows), typeof(BookmarksPanel),
        typeof(ErrorDialog), typeof(AssemblyListsDialog), typeof(FrameworkPicker), typeof(Search), typeof(ExportSetup), typeof(AssemblyPicker),
        typeof(MetadataFilters), typeof(MainLayout), typeof(CodeViewState), typeof(MetadataGridState), typeof(TableColumns), typeof(FoldedGroups),
        typeof(TreeViewState<>),
    ];

    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void The_app_state_reaches_no_object_of_the_screen_over_it()
    {
        var d = AppStateDriver.Opened(_temp.Settings());
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);
        OpenBookmarks(d.Ui);
        d.Analyze(d.Fixture("Greeter"));
        ScopeSearch(d.Ui, d.Node(AppStateDriver.FixturesNamespace));
        d.Ui.BookmarkColumns.Restore([1f, 1f, 4f, 1f]);
        d.Settle();
        SaveSettings(d.AppState);

        var reached = FindScreenObject(d.AppState, d.Ui);

        Assert.Null(reached);
    }

    /// <summary>The path from <paramref name="appState"/> to the first object of
    /// <paramref name="screen"/> it reaches, or null.</summary>
    private static string? FindScreenObject(AppState appState, UiState screen)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var queue = new Queue<(object Value, string Path)>();
        queue.Enqueue((appState, nameof(AppState)));
        while (queue.TryDequeue(out var next))
        {
            if (IsOfScreen(next.Value, screen))
                return next.Path;
            if (!next.Value.GetType().IsValueType && !seen.Add(next.Value))
                continue;
            foreach (var reference in ListReferences(next.Value, next.Path))
                queue.Enqueue(reference);
        }
        return null;
    }

    private static bool IsOfScreen(object value, UiState screen)
    {
        if (ReferenceEquals(value, screen.Layout))
            return true;
        var type = value.GetType();
        if (type.IsGenericType)
            type = type.GetGenericTypeDefinition();
        return ScreenTypes.Contains(type);
    }

    private static IEnumerable<(object Value, string Path)> ListReferences(object value, string path)
    {
        if (IsOpaque(value))
            yield break;
        if (value is Array array)
        {
            foreach (var element in ListElements(array, path))
                yield return element;
            yield break;
        }
        if (!WalkedAssemblies.Contains(value.GetType().Assembly))
            yield break;
        for (var type = value.GetType(); type != null; type = type.BaseType)
        {
            foreach (var field in type.GetFields(InstanceFields))
            {
                if (field.FieldType.IsPrimitive || field.FieldType.IsPointer)
                    continue;
                var held = field.GetValue(value);
                if (held != null)
                    yield return (held, $"{path}.{field.Name}");
            }
        }
    }

    private static IEnumerable<(object Value, string Path)> ListElements(Array array, string path)
    {
        var element = array.GetType().GetElementType()!;
        if (element.IsPrimitive || element.IsEnum || element == typeof(string))
            yield break;
        for (var i = 0; i < array.Length; i++)
        {
            var held = array.GetValue(i);
            if (held != null)
                yield return (held, $"{path}[{i}]");
        }
    }

    private static bool IsOpaque(object value)
    {
        if (value is string)
            return true;
        if (value is MemberInfo)
            return true;
        if (value is Assembly)
            return true;
        if (value is Module)
            return true;
        return value is Thread;
    }
}
