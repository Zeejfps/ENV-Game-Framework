using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

internal static class Trees
{
    public const string FixturesNamespace = "ZGF.ILSpy.Tests.Fixtures";

    public static string TestAssemblyPath => typeof(Trees).Assembly.Location;

    public static string CoreLibPath => typeof(object).Assembly.Location;

    public static BrowseNode Child(this BrowseNode node, string text) =>
        node.Children.FirstOrDefault(c => c.Text == text) ??
        throw new InvalidOperationException($"'{node.Text}' has no child '{text}': {string.Join(", ", node.Children.Select(c => c.Text))}");

    public static BrowseNode Fixtures(this LoadedAssembly assembly) => assembly.Root.Child(FixturesNamespace);

    public static BrowseNode Calculator(this LoadedAssembly assembly) => assembly.Fixtures().Child("Calculator<T>");

    public static (AssemblyList List, LoadedAssembly Assembly) OpenTestAssembly()
    {
        var list = new AssemblyList();
        return (list, list.Open(TestAssemblyPath));
    }

    public static int LineContaining(this DecompiledDocument document, string text)
    {
        for (var i = 0; i < document.Lines.Length; i++)
        {
            if (document.Lines[i].Text.Contains(text, StringComparison.Ordinal)) return i;
        }
        return -1;
    }

    public static DocumentSpan SpanAt(this DocumentLine line, string token)
    {
        var start = line.Text.IndexOf(token, StringComparison.Ordinal);
        foreach (var span in line.Spans)
        {
            if (span.Start == start && span.Length == token.Length) return span;
        }
        throw new InvalidOperationException($"No span '{token}' in '{line.Text}'");
    }

    public static IEnumerable<(string Text, DocumentSpan Span)> SpansOf(this DecompiledDocument document)
    {
        foreach (var line in document.Lines)
        {
            var text = line.Text;
            var spans = line.Spans.ToArray();
            foreach (var span in spans)
                yield return (text.Substring(span.Start, span.Length), span);
        }
    }
}
