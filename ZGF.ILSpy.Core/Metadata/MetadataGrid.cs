using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata;

/// <summary>One column of a <see cref="MetadataGrid"/>.</summary>
/// <param name="Name">ILSpy's header for it.</param>
/// <param name="IsToken">Whether its cells are metadata tokens that <see cref="MetadataGrid.Link"/>
/// can follow.</param>
/// <param name="IsNumeric">Whether it holds numbers, which read best aligned to the right.</param>
/// <param name="Flags">For a column of flags, how its bits divide into groups, which a
/// <see cref="FilterState"/> filters on.</param>
public sealed record MetadataColumn(string Name, bool IsToken, bool IsNumeric, FlagsSchema? Flags = null);

/// <summary>
/// What one column of a <see cref="MetadataGrid"/> is filtered by, as ILSpy's header filters do:
/// <see cref="Text"/> — a <c>/regex/</c>, a comparison such as <c>&gt;= 0x100</c> for a number, or
/// else a part of the cell's text, ignoring case — and for a flags column its <see cref="Flags"/>.
/// </summary>
public sealed class MetadataColumnFilter
{
    public string Text { get; set; } = "";

    public FilterState? Flags { get; init; }

    public bool IsActive => Text.Length > 0 || Flags is { IsEmpty: false };
}

/// <summary>What is shown under the selected row of a <see cref="MetadataGrid"/>.</summary>
public abstract record MetadataRowDetails;

/// <summary>A decoded text, with the file extension of the language it is in, if known.</summary>
public sealed record MetadataTextDetails(string Text, string? Extension) : MetadataRowDetails;

/// <summary>A decoded list, as a table.</summary>
public sealed record MetadataTableDetails(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows) : MetadataRowDetails;

/// <summary>One named flag in a <see cref="MetadataFlagGroup"/>, and whether it is set.</summary>
public readonly record struct MetadataFlag(string Name, bool IsSet);

/// <summary>A part of a flags value: for a mutually exclusive range, the one member it holds;
/// otherwise every independent flag, set or not.</summary>
public sealed record MetadataFlagGroup(string? Header, bool IsChoice, IReadOnlyList<MetadataFlag> Flags);

/// <summary>What hovering a cell explains: text, or a flags value broken down into its groups.</summary>
public sealed record MetadataTooltip(string? Text, IReadOnlyList<MetadataFlagGroup> Groups);

/// <summary>A row of a table of the metadata a grid lists, as a token cell names it.</summary>
public readonly record struct MetadataAddress(TableIndex Table, int Row);

/// <summary>
/// The rows of a metadata node as ILSpy shows them in a grid: a header, a directory, a table or a
/// heap, read through its <see cref="GridSchema"/>. Cell text, links, tooltips and details are
/// worked out once and kept; read them on one thread at a time.
/// </summary>
public sealed class MetadataGrid
{
    private static readonly ConcurrentDictionary<string, Regex?> Patterns = new();

    private readonly LoadedModule _module;
    private readonly MetadataFile _file;
    private readonly GridSchema _schema;
    private readonly IReadOnlyList<object> _rows;
    private readonly IReadOnlyList<GridColumn> _columns;
    private readonly Dictionary<int, MetadataTooltip?> _tooltips = new();
    private readonly Dictionary<int, MetadataAddress?> _links = new();
    private readonly Dictionary<TableIndex, bool> _listed = new();
    private readonly Dictionary<int, MetadataRowDetails?> _details = new();
    private string?[]? _text;
    private (string FirstLine, int MoreLines)?[]? _lines;

    /// <summary>The grid titled <paramref name="title"/> of what <paramref name="schema"/> reads from
    /// <paramref name="file"/>, which is <paramref name="module"/>'s own metadata or its debug metadata.</summary>
    internal MetadataGrid(string title, LoadedModule module, MetadataFile file, GridSchema schema)
    {
        Title = title;
        _module = module;
        _file = file;
        _schema = schema;
        _rows = schema.LoadRows();
        _columns = schema.Columns;
        Columns = _columns.Select(static c => c.Column).ToArray();
    }

    /// <summary>What names the kind of rows, the same for a table in any assembly: where a
    /// column's flags filter is kept.</summary>
    public string Key => _schema.Key;

    /// <summary>Whether the selected row shows its <see cref="Details"/> under it, as a debug
    /// information's decoded value does.</summary>
    public bool HasDetails => _schema.Details != null;

    /// <summary>What the grid lists, as its node is labelled.</summary>
    public string Title { get; }

    public IReadOnlyList<MetadataColumn> Columns { get; }

    public int RowCount => _rows.Count;

    /// <summary>The text of a cell: numbers in the column's format, often hex.</summary>
    public string Text(int row, int column)
    {
        var text = _text ??= new string?[_rows.Count * _columns.Count];
        var index = row * _columns.Count + column;
        return text[index] ??= Format(Value(row, column), _columns[column].Format);
    }

    /// <summary>The rows of <paramref name="order"/>, or of the table in its own order, that pass
    /// every one of <paramref name="filters"/>, one per column or null.</summary>
    public int[] Filter(int[]? order, IReadOnlyList<MetadataColumnFilter?> filters)
    {
        var active = Enumerable.Range(0, Math.Min(filters.Count, _columns.Count)).Where(c => filters[c] is { IsActive: true }).ToArray();
        var rows = order ?? Enumerable.Range(0, _rows.Count).ToArray();
        if (active.Length == 0) return rows;
        return rows.Where(row => Array.TrueForAll(active, c => Matches(row, c, filters[c]!))).ToArray();
    }

    /// <summary>What is shown under <paramref name="row"/> while it is selected; null when it has
    /// nothing.</summary>
    public MetadataRowDetails? Details(int row)
    {
        var details = _schema.Details;
        if (details == null) return null;
        if (_details.TryGetValue(row, out var known)) return known;
        MetadataRowDetails? result;
        try
        {
            result = details(_rows[row]);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            result = null;
        }
        _details[row] = result;
        return result;
    }

    /// <summary>A cell's text up to its first line break, and how many lines follow it — none for
    /// text on one line.</summary>
    public (string FirstLine, int MoreLines) Lines(int row, int column)
    {
        var lines = _lines ??= new (string, int)?[_rows.Count * _columns.Count];
        var index = row * _columns.Count + column;
        var known = lines[index];
        if (known != null) return known.Value;
        var text = Text(row, column).TrimEnd('\r', '\n');
        var end = text.IndexOf('\n');
        var result = end < 0 ? (text, 0) : (text[..end].TrimEnd('\r'), text.AsSpan(end).Count('\n'));
        lines[index] = result;
        return result;
    }

    /// <summary>The order that sorts the rows by <paramref name="column"/>, as row indices; ties
    /// keep the table's order.</summary>
    public int[] Order(int column, bool descending)
    {
        var keys = new object?[_rows.Count];
        for (var row = 0; row < keys.Length; row++)
            keys[row] = Value(row, column);
        var order = Enumerable.Range(0, keys.Length).ToArray();
        var sign = descending ? -1 : 1;
        Array.Sort(order, (a, b) =>
        {
            var compared = Compare(keys[a], keys[b]);
            return compared != 0 ? sign * compared : a.CompareTo(b);
        });
        return order;
    }

    /// <summary>What hovering a cell explains, or null when its column has nothing to say.</summary>
    public MetadataTooltip? Tooltip(int row, int column)
    {
        var info = _columns[column];
        if (info.Column.Flags == null && info.Tooltip == null) return null;
        var index = row * _columns.Count + column;
        if (_tooltips.TryGetValue(index, out var known)) return known;
        object? value;
        try
        {
            value = info.Column.Flags != null
                ? ExplainFlags(info.Column.Flags, Value(row, column))
                : info.Tooltip!(_rows[row], row + 1);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            value = null;
        }
        var result = value switch
        {
            MetadataTooltip given => given,
            string text when !string.IsNullOrWhiteSpace(text) => new MetadataTooltip(text, []),
            _ => null,
        };
        _tooltips[index] = result;
        return result;
    }

    /// <summary>The row a token cell points at, in the table of this metadata it names; null for a
    /// cell that is not a token, a nil token or a table this metadata lists no rows of. A row past
    /// the table's end, as the end of an empty member list is, leads to the last row.</summary>
    public MetadataAddress? Link(int row, int column)
    {
        if (!_columns[column].Column.IsToken) return null;
        var index = row * _columns.Count + column;
        if (_links.TryGetValue(index, out var known)) return known;
        var result = FindLink(row, column);
        _links[index] = result;
        return result;
    }

    private MetadataAddress? FindLink(int row, int column)
    {
        if (Value(row, column) is not int token || (token & 0xFFFFFF) == 0) return null;
        var table = (TableIndex)(token >>> 24);
        if (!IsListed(table)) return null;
        var count = _file.Metadata.GetTableRowCount(table);
        return new MetadataAddress(table, Math.Min(token & 0xFFFFFF, count) - 1);
    }

    /// <summary>Whether this metadata's tables list <paramref name="table"/> as a grid of rows, as they
    /// do a table they can read that has rows.</summary>
    private bool IsListed(TableIndex table)
    {
        if (_listed.TryGetValue(table, out var known)) return known;
        var listed = Enum.IsDefined(table) && _file.Metadata.GetTableRowCount(table) > 0 && MetadataTables.Create(table, _file) != null;
        _listed[table] = listed;
        return listed;
    }

    /// <summary>The row of <paramref name="token"/>'s table it is, or -1 when this does not list
    /// that table.</summary>
    public int RowOf(int token) => _schema.Table != null && (int)_schema.Table == token >>> 24 ? (token & 0xFFFFFF) - 1 : -1;

    /// <summary>
    /// What the row stands for in the decompiled code, as the double-click on it goes to: the type,
    /// member or assembly it is — a reference resolved to its definition. Null for a row that is
    /// none, or a row of debug metadata.
    /// </summary>
    public CodeReference? Definition(int row)
    {
        var file = _file;
        if (!ReferenceEquals(file, _module.File) || _schema.Table == null) return null;
        var token = (int)_schema.Table << 24 | row + 1;
        var handle = MetadataTokens.EntityHandle(token);
        var kind = CodeReference.KindOf(file.Metadata, handle);
        if (kind == null) return null;
        var name = handle.Kind == HandleKind.AssemblyDefinition ? _module.Name
            : _schema.NameColumn >= 0 ? Text(row, _schema.NameColumn)
            : token.ToString("X8");
        return CodeReference.ToEntity(kind.Value, name, AssemblyPath.Of(file), handle);
    }

    private bool Matches(int row, int column, MetadataColumnFilter filter)
    {
        var value = Value(row, column);
        if (value is null) return false;
        if (filter.Flags is { IsEmpty: false } flags && !flags.Matches(Bits(value)))
            return false;
        var text = filter.Text;
        if (text.Length == 0) return true;
        var pattern = Pattern(text);
        if (pattern != null)
            return pattern.IsMatch(value.ToString() ?? "");
        if (_columns[column].Column.IsNumeric)
        {
            var compared = Compare(value, text);
            if (compared != null)
                return compared.Value;
        }
        return Text(row, column).Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>What the flags set in <paramref name="cell"/> mean; null for an empty cell.</summary>
    private static MetadataTooltip? ExplainFlags(FlagsSchema flags, object? cell)
    {
        if (cell == null)
            return null;
        return flags.Explain(Bits(cell));
    }

    private static Regex? Pattern(string text)
    {
        if (text.Length < 2 || text[0] != '/' || text[^1] != '/') return null;
        return Patterns.GetOrAdd(text, static t =>
        {
            try
            {
                return new Regex(t[1..^1], RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            catch (ArgumentException)
            {
                return null;
            }
        });
    }

    /// <summary>Whether <paramref name="value"/> passes a comparison such as <c>&gt;= 0x10</c>; null
    /// when <paramref name="text"/> is not one, as a bare number is not.</summary>
    private static bool? Compare(object value, string text)
    {
        var (op, operand) = text switch
        {
            _ when text.StartsWith(">=", StringComparison.Ordinal) => (">=", text[2..].Trim()),
            _ when text.StartsWith("<=", StringComparison.Ordinal) => ("<=", text[2..].Trim()),
            [('>' or '<' or '='), ..] => (text[..1], text[1..].Trim()),
            _ => (null, text),
        };
        if (op == null) return null;
        var parsed = operand.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? long.TryParse(operand.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var number)
            : long.TryParse(operand, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
        if (!parsed) return null;
        long actual;
        try
        {
            actual = Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is InvalidCastException or OverflowException or FormatException)
        {
            return null;
        }
        return op switch
        {
            "=" => actual == number,
            "<" => actual < number,
            ">" => actual > number,
            "<=" => actual <= number,
            _ => actual >= number,
        };
    }

    private object? Value(int row, int column)
    {
        try
        {
            return _columns[column].Value(_rows[row], row + 1);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>Whether <paramref name="e"/> is how metadata that cannot be read fails, which leaves
    /// its cell empty.</summary>
    private static bool IsUnreadable(Exception e) => e is BadImageFormatException or ArgumentException or InvalidOperationException;

    private static string Format(object? value, string? format)
    {
        if (value is null) return "";
        if (!string.IsNullOrEmpty(format))
        {
            if (value is Enum enumValue)
                value = Convert.ChangeType(enumValue, enumValue.GetTypeCode(), CultureInfo.InvariantCulture);
            if (value is IFormattable formattable)
                return formattable.ToString(format, CultureInfo.InvariantCulture);
        }
        return value.ToString() ?? "";
    }

    private static int Compare(object? a, object? b)
    {
        if (a is null || b is null) return a is null ? b is null ? 0 : -1 : 1;
        if (a.GetType() == b.GetType() && a is IComparable comparable) return comparable.CompareTo(b);
        if (IsNumber(a) && IsNumber(b))
            return Convert.ToDecimal(a, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(b, CultureInfo.InvariantCulture));
        return string.CompareOrdinal(a.ToString(), b.ToString());
    }

    private static bool IsNumber(object value) => value is Enum || Type.GetTypeCode(value.GetType()) is >= TypeCode.SByte and <= TypeCode.Decimal;

    private static uint Bits(object value) => unchecked((uint)Convert.ToInt64(value, CultureInfo.InvariantCulture));
}
