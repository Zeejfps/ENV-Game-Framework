using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace ZGF.ILSpy.Core.Metadata;

/// <summary>One column of a <see cref="MetadataGrid"/>.</summary>
/// <param name="Name">ILSpy's header for it: the name of the row property it shows.</param>
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

/// <summary>When a <see cref="MetadataGrid"/> shows rows' <see cref="MetadataGrid.Details"/>.</summary>
public enum MetadataDetailsMode
{
    None,

    /// <summary>Each row that has details folds them out under it on demand, as a header's
    /// flags do.</summary>
    Foldable,

    /// <summary>The selected row shows its details under it.</summary>
    WhenSelected,
}

/// <summary>What is shown under a row of a <see cref="MetadataGrid"/>.</summary>
public abstract record MetadataRowDetails;

/// <summary>A flags value bit by bit: whether each is set, and what it means.</summary>
public sealed record MetadataBitDetails(IReadOnlyList<MetadataFlag> Bits) : MetadataRowDetails;

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

/// <summary>Where a token cell leads: a row of a table node.</summary>
public readonly record struct MetadataTarget(BrowseNode Node, int Row);

/// <summary>
/// The rows of a metadata node as ILSpy shows them in a grid: a header, a directory, a table or a
/// heap. Columns are the public properties of ILSpy's row types, in declaration order; a
/// <c>{Column}Tooltip</c> property explains its column's cells. Cell text is formatted once and
/// kept; read it on one thread at a time.
/// </summary>
public sealed class MetadataGrid
{
    private static readonly ConcurrentDictionary<Type, Descriptor[]> Descriptors = new();
    private static readonly ConcurrentDictionary<string, Regex?> Patterns = new();

    private readonly MetadataGridNode _owner;
    private readonly IReadOnlyList<object> _rows;
    private readonly Descriptor[] _descriptors;
    private readonly PropertyInfo? _details;
    private string?[]? _text;

    internal MetadataGrid(MetadataGridNode owner, Type entryType, IReadOnlyList<object> rows)
    {
        _owner = owner;
        _rows = rows;
        _descriptors = Descriptors.GetOrAdd(entryType, Describe);
        Columns = _descriptors.Select(static d => d.Column).ToArray();
        Key = entryType.FullName ?? entryType.Name;
        _details = entryType.GetProperty(nameof(Entry.RowDetails), BindingFlags.Public | BindingFlags.Instance);
        DetailsMode = _details == null ? MetadataDetailsMode.None : entryType == typeof(Entry) ? MetadataDetailsMode.Foldable : MetadataDetailsMode.WhenSelected;
    }

    /// <summary>What names the kind of rows, the same for a table in any assembly: where a
    /// column's flags filter is kept.</summary>
    public string Key { get; }

    /// <summary>When rows show their <see cref="Details"/>: folded out on demand, as a header's
    /// flags are, or under the selected row, as a debug information's decoded value is.</summary>
    public MetadataDetailsMode DetailsMode { get; }

    /// <summary>What the grid lists, as its node is labelled.</summary>
    public string Title => _owner.Text;

    public IReadOnlyList<MetadataColumn> Columns { get; }

    public int RowCount => _rows.Count;

    /// <summary>The text of a cell: numbers in the column's format, often hex.</summary>
    public string Text(int row, int column)
    {
        var text = _text ??= new string?[_rows.Count * _descriptors.Length];
        var index = row * _descriptors.Length + column;
        return text[index] ??= Format(Value(row, column), _descriptors[column].Format);
    }

    /// <summary>The rows of <paramref name="order"/>, or of the table in its own order, that pass
    /// every one of <paramref name="filters"/>, one per column or null.</summary>
    public int[] Filter(int[]? order, IReadOnlyList<MetadataColumnFilter?> filters)
    {
        var active = new List<(int Column, MetadataColumnFilter Filter, CompiledFilter? Flags)>();
        for (var column = 0; column < filters.Count && column < _descriptors.Length; column++)
        {
            if (filters[column] is { IsActive: true } filter)
                active.Add((column, filter, filter.Flags is { IsEmpty: false } flags ? CompiledFilter.Compile(flags) : null));
        }
        var rows = order ?? Enumerable.Range(0, _rows.Count).ToArray();
        if (active.Count == 0) return rows;
        return rows.Where(row => active.TrueForAll(a => Matches(row, a.Column, a.Filter.Text, a.Flags))).ToArray();
    }

    /// <summary>What is shown under <paramref name="row"/>, as <see cref="DetailsMode"/> says when;
    /// null when it has nothing.</summary>
    public MetadataRowDetails? Details(int row)
    {
        if (_details == null) return null;
        object? value;
        try
        {
            value = _details.GetValue(_rows[row]);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
        return value switch
        {
            IList<BitEntry> bits => new MetadataBitDetails(bits.Select(static b => new MetadataFlag(b.Meaning, b.Value)).ToArray()),
            TextBlobDetail text => new MetadataTextDetails(text.Text, text.HighlightExtension),
            IReadOnlyList<HoistedLocalScopeDetail> scopes => Table(scopes, ("Start Offset", static s => s.StartOffset), ("Length", static s => s.Length)),
            IReadOnlyList<CompilationOptionDetail> options => Table(options, ("Name", static o => o.Name), ("Value", static o => o.Value)),
            IReadOnlyList<MetadataReferenceDetail> references => Table(references, ("File Name", static r => r.FileName), ("Aliases", static r => r.Aliases),
                ("Flags", static r => r.Flags), ("Timestamp", static r => r.Timestamp), ("File Size", static r => r.FileSize), ("MVID", static r => r.Mvid)),
            IReadOnlyList<TupleElementNameDetail> names => Table(names, ("Element Name", static n => n.ElementName)),
            _ => null,
        };

        static MetadataTableDetails Table<T>(IReadOnlyList<T> rows, params (string Header, Func<T, object?> Value)[] columns) =>
            new(columns.Select(static c => c.Header).ToArray(),
                rows.Select(r => (IReadOnlyList<string>)columns.Select(c => c.Value(r)?.ToString() ?? "").ToArray()).ToArray());
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
        if (_descriptors[column].Tooltip is not { } tooltip) return null;
        object? value;
        try
        {
            value = tooltip.GetValue(_rows[row]);
        }
        catch (Exception e) when (e is TargetInvocationException or BadImageFormatException)
        {
            return null;
        }
        return value switch
        {
            FlagsTooltip flags => new MetadataTooltip(null, flags.Groups.Select(Group).ToArray()),
            _ when value?.ToString() is { } text && !string.IsNullOrWhiteSpace(text) => new MetadataTooltip(text, []),
            _ => null,
        };
    }

    /// <summary>The row a token cell points at, in the table of this metadata it names; null for a
    /// cell that is not a token, a nil token or a table this metadata does not have. A row past
    /// the table's end, as the end of an empty member list is, leads to the last row.</summary>
    public MetadataTarget? Link(int row, int column)
    {
        if (!_descriptors[column].Column.IsToken || Value(row, column) is not int token || (token & 0xFFFFFF) == 0) return null;
        if (_owner.Folder?.FindTable((TableIndex)(token >>> 24)) is not { } table) return null;
        var count = table.File.Metadata.GetTableRowCount(table.Table!.Value);
        return new MetadataTarget(table, Math.Min(token & 0xFFFFFF, count) - 1);
    }

    /// <summary>The row of <paramref name="token"/>'s table it is, or -1 when this does not list
    /// that table.</summary>
    public int RowOf(int token) => _owner.Table is { } table && (int)table == token >>> 24 ? (token & 0xFFFFFF) - 1 : -1;

    /// <summary>
    /// What the row stands for in the decompiled code, as the double-click on it goes to: the type,
    /// member or assembly its <c>Token</c> names — a reference resolved to its definition. Null for
    /// a row that names none, or a row of debug metadata.
    /// </summary>
    public CodeReference? Definition(int row)
    {
        var file = _owner.File;
        if (!ReferenceEquals(file, _owner.Assembly.File)) return null;
        var token = Array.FindIndex(_descriptors, static d => d.Column.Name == "Token") is >= 0 and var column ? Value(row, column) as int? : null;
        if (token is not { } value || value == 0) return null;
        var handle = MetadataTokens.EntityHandle(value);
        var name = Array.FindIndex(_descriptors, static d => d.Column.Name == "Name") is >= 0 and var named ? Text(row, named) : value.ToString("X8");
        var kind = handle.Kind switch
        {
            HandleKind.TypeDefinition or HandleKind.TypeReference or HandleKind.TypeSpecification => CodeReferenceKind.Type,
            HandleKind.MethodDefinition or HandleKind.MethodSpecification => CodeReferenceKind.Method,
            HandleKind.FieldDefinition => CodeReferenceKind.Field,
            HandleKind.PropertyDefinition => CodeReferenceKind.Property,
            HandleKind.EventDefinition => CodeReferenceKind.Event,
            HandleKind.MemberReference => file.Metadata.GetMemberReference((MemberReferenceHandle)handle).GetKind() == MemberReferenceKind.Field
                ? CodeReferenceKind.Field
                : CodeReferenceKind.Method,
            HandleKind.AssemblyReference => CodeReferenceKind.Assembly,
            HandleKind.AssemblyDefinition => CodeReferenceKind.Assembly,
            _ => (CodeReferenceKind?)null,
        };
        if (kind is not { } found) return null;
        return handle.Kind == HandleKind.AssemblyDefinition
            ? CodeReference.ToAssembly(_owner.Assembly.Name, file.FileName)
            : CodeReference.ToEntity(found, name, file.FileName, handle);
    }

    private bool Matches(int row, int column, string text, CompiledFilter? flags)
    {
        var value = Value(row, column);
        if (value is null) return false;
        if (flags != null && !flags.Matches(unchecked((uint)Convert.ToInt64(value, CultureInfo.InvariantCulture))))
            return false;
        if (text.Length == 0) return true;
        if (Pattern(text) is { } pattern)
            return pattern.IsMatch(value.ToString() ?? "");
        if (_descriptors[column].Column.IsNumeric && Compare(value, text) is { } compared)
            return compared;
        return Text(row, column).Contains(text, StringComparison.OrdinalIgnoreCase);
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
            return _descriptors[column].Property.GetValue(_rows[row]);
        }
        catch (TargetInvocationException e) when (e.InnerException is BadImageFormatException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

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

    private static MetadataFlagGroup Group(FlagGroup group) =>
        group is SingleChoiceGroup single
            ? new MetadataFlagGroup(group.Header, true, single.SelectedFlag.Name is { } name ? [new MetadataFlag(name, true)] : [])
            : new MetadataFlagGroup(group.Header, false, group.Flags.Where(static f => f.Value != -1).Select(static f => new MetadataFlag(f.Name, f.IsSelected)).ToArray());

    private static Descriptor[] Describe(Type entryType)
    {
        var properties = entryType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var list = new List<Descriptor>();
        foreach (var property in properties)
        {
            if (property.GetIndexParameters().Length > 0 || property.Name == nameof(Entry.RowDetails) || property.PropertyType == typeof(IList<BitEntry>)
                || property.Name.EndsWith("Tooltip", StringComparison.Ordinal))
                continue;
            var info = property.GetCustomAttribute<ColumnInfoAttribute>();
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var numeric = type.IsEnum || Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal;
            var tooltip = entryType.GetProperty(property.Name + "Tooltip", BindingFlags.Public | BindingFlags.Instance);
            var flags = type.IsEnum && type.IsDefined(typeof(FlagsAttribute), false) ? FlagsSchemaInferer.For(type) : null;
            list.Add(new Descriptor(new MetadataColumn(property.Name, info?.Kind == ColumnKind.Token, numeric, flags), property, info?.Format, tooltip));
        }
        return list.ToArray();
    }

    private sealed record Descriptor(MetadataColumn Column, PropertyInfo Property, string? Format, PropertyInfo? Tooltip);
}
