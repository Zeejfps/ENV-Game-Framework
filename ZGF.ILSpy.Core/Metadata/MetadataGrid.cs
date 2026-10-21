using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ZGF.ILSpy.Core.Metadata;

/// <summary>One column of a <see cref="MetadataGrid"/>.</summary>
/// <param name="Name">ILSpy's header for it: the name of the row property it shows.</param>
/// <param name="IsToken">Whether its cells are metadata tokens that <see cref="MetadataGrid.Link"/>
/// can follow.</param>
/// <param name="IsNumeric">Whether it holds numbers, which read best aligned to the right.</param>
public sealed record MetadataColumn(string Name, bool IsToken, bool IsNumeric);

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

    private readonly MetadataGridNode _owner;
    private readonly IReadOnlyList<object> _rows;
    private readonly Descriptor[] _descriptors;
    private string?[]? _text;

    internal MetadataGrid(MetadataGridNode owner, Type entryType, IReadOnlyList<object> rows)
    {
        _owner = owner;
        _rows = rows;
        _descriptors = Descriptors.GetOrAdd(entryType, Describe);
        Columns = _descriptors.Select(static d => d.Column).ToArray();
    }

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
            list.Add(new Descriptor(new MetadataColumn(property.Name, info?.Kind == ColumnKind.Token, numeric), property, info?.Format, tooltip));
        }
        return list.ToArray();
    }

    private sealed record Descriptor(MetadataColumn Column, PropertyInfo Property, string? Format, PropertyInfo? Tooltip);
}
