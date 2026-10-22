// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata;

/// <summary>One column of a <see cref="GridSchema"/>: its cell value and tooltip, read from a row
/// and its 1-based RID.</summary>
internal sealed record GridColumn(MetadataColumn Column, string? Format, Func<object, int, object?> Value, Func<object, int, object?>? Tooltip);

/// <summary>What a <see cref="MetadataGrid"/> lists: its columns, and how to read its rows.</summary>
internal abstract class GridSchema(string key, TableIndex? table)
{
    private readonly List<GridColumn> _columns = [];

    /// <summary>What names the kind of rows, the same for a table in any assembly.</summary>
    public string Key { get; } = key;

    /// <summary>The table the rows are of, for a metadata table.</summary>
    public TableIndex? Table { get; } = table;

    public IReadOnlyList<GridColumn> Columns => _columns;

    /// <summary>The column naming each row, or -1.</summary>
    public int NameColumn { get; private protected set; } = -1;

    /// <summary>What is shown under a selected row, for a grid that shows something.</summary>
    public Func<object, MetadataRowDetails?>? Details { get; private protected set; }

    /// <summary>Every row, in order.</summary>
    public abstract IReadOnlyList<object> LoadRows();

    private protected void Add(GridColumn column) => _columns.Add(column);

    /// <summary>
    /// What the entity <paramref name="handle"/> points at is, e.g. <c>(AssemblyReference) System.Runtime, ...</c>;
    /// null for a nil handle.
    /// </summary>
    public static string? Describe(MetadataFile module, EntityHandle handle)
    {
        if (handle.IsNil)
            return null;
        ITextOutput output = new PlainTextOutput();
        var context = new MetadataGenericContext(default(TypeDefinitionHandle), module.Metadata);
        var metadata = module.Metadata;
        switch (handle.Kind)
        {
            case HandleKind.ModuleDefinition:
                output.Write(metadata.GetString(metadata.GetModuleDefinition().Name));
                output.Write(" (this module)");
                break;
            case HandleKind.ModuleReference:
                output.Write(metadata.GetString(metadata.GetModuleReference((ModuleReferenceHandle)handle).Name));
                break;
            case HandleKind.AssemblyReference:
                output.Write(new ICSharpCode.Decompiler.Metadata.AssemblyReference(module, (AssemblyReferenceHandle)handle).ToString());
                break;
            case HandleKind.Parameter:
                var param = metadata.GetParameter((ParameterHandle)handle);
                output.Write(param.SequenceNumber + " - " + metadata.GetString(param.Name));
                break;
            case HandleKind.EventDefinition:
                output.Write(metadata.GetString(metadata.GetEventDefinition((EventDefinitionHandle)handle).Name));
                break;
            case HandleKind.PropertyDefinition:
                output.Write(metadata.GetString(metadata.GetPropertyDefinition((PropertyDefinitionHandle)handle).Name));
                break;
            case HandleKind.AssemblyDefinition:
                output.Write(metadata.GetString(metadata.GetAssemblyDefinition().Name));
                output.Write(" (this assembly)");
                break;
            case HandleKind.AssemblyFile:
                output.Write(metadata.GetString(metadata.GetAssemblyFile((AssemblyFileHandle)handle).Name));
                break;
            case HandleKind.GenericParameter:
                output.Write(metadata.GetString(metadata.GetGenericParameter((GenericParameterHandle)handle).Name));
                break;
            case HandleKind.ManifestResource:
                output.Write(metadata.GetString(metadata.GetManifestResource((ManifestResourceHandle)handle).Name));
                break;
            case HandleKind.Document:
                output.Write(metadata.GetString(metadata.GetDocument((DocumentHandle)handle).Name));
                break;
            default:
                handle.WriteTo(module, output, context);
                break;
        }
        return "(" + handle.Kind + ") " + output;
    }
}

/// <summary>A <see cref="GridSchema"/> over rows of <typeparamref name="TRow"/>, built a column at a
/// time. A table's starts with its RID and Token.</summary>
internal sealed class GridSchema<TRow> : GridSchema
{
    private readonly MetadataFile _file;
    private readonly Func<IEnumerable<TRow>> _rows;

    public GridSchema(string key, MetadataFile file, Func<IEnumerable<TRow>> rows)
        : base(key, null)
    {
        _file = file;
        _rows = rows;
    }

    public GridSchema(TableIndex table, MetadataFile file, Func<IEnumerable<TRow>> rows)
        : base(table.ToString(), table)
    {
        _file = file;
        _rows = rows;
        Add("RID", null, (_, rid) => rid, null);
        Add("Token", "X8", (_, rid) => (int)table << 24 | rid, null);
    }

    public override IReadOnlyList<object> LoadRows() => _rows().Select(static r => (object)r!).ToArray();

    /// <summary>A column of what <paramref name="get"/> reads, in <paramref name="format"/>.</summary>
    public GridSchema<TRow> Value<T>(string name, Func<TRow, T> get, string? format = null, Func<TRow, object?>? tooltip = null) =>
        Add(name, format, (r, _) => get(r), tooltip == null ? null : (r, _) => tooltip(r));

    /// <summary>Where each row starts in the file.</summary>
    public GridSchema<TRow> Offset()
    {
        var table = Table!.Value;
        var metadata = _file.Metadata;
        var start = _file.MetadataOffset + metadata.GetTableMetadataOffset(table);
        var size = metadata.GetTableRowSize(table);
        return Add("Offset", "X8", (_, rid) => start + size * (rid - 1), null);
    }

    /// <summary>A string of the #Strings heap, explained by its offset.</summary>
    public GridSchema<TRow> String(string name, Func<TRow, StringHandle> get) =>
        Value(name, r => _file.Metadata.GetString(get(r)),
            tooltip: r => $"{MetadataTokens.GetHeapOffset(get(r)):X} \"{_file.Metadata.GetString(get(r))}\"");

    /// <summary>The row's name: a <see cref="String"/> column the row is known by.</summary>
    public GridSchema<TRow> Name(Func<TRow, StringHandle> get)
    {
        NameColumn = Columns.Count;
        return String("Name", get);
    }

    /// <summary>A token that links to its row, explained by what it points at unless
    /// <paramref name="tooltip"/> says otherwise.</summary>
    public GridSchema<TRow> Handle(string name, Func<TRow, EntityHandle> get, Func<TRow, object?>? tooltip = null) =>
        Add(name, "X8", (r, _) => MetadataTokens.GetToken(get(r)),
            tooltip == null ? (r, _) => Describe(_file, get(r)) : (r, _) => tooltip(r), isToken: true);

    /// <summary>The start of a run of rows of <paramref name="table"/>, as a token, explained by the
    /// row it starts at or, for an empty run, by <paramref name="empty"/>.</summary>
    public GridSchema<TRow> List(string name, TableIndex table, Func<TRow, int> get, Func<TRow, EntityHandle> first, string empty) =>
        Add(name, "X8", (r, _) => (int)table << 24 | get(r),
            (r, _) => first(r) is { IsNil: false } handle ? Describe(_file, handle) : empty, isToken: true);

    /// <summary>The offset of a blob in the #Blob heap.</summary>
    public GridSchema<TRow> Blob(string name, Func<TRow, BlobHandle> get, Func<TRow, object?>? tooltip = null) =>
        Value(name, r => MetadataTokens.GetHeapOffset(get(r)), "X8", tooltip);

    /// <summary>The offset of a signature blob, explained by the row it is the signature of.</summary>
    public GridSchema<TRow> Signature(string name, Func<TRow, BlobHandle> get)
    {
        var table = Table!.Value;
        return Add(name, "X8", (r, _) => MetadataTokens.GetHeapOffset(get(r)), (_, rid) => Describe(_file, MetadataTokens.EntityHandle(table, rid)));
    }

    /// <summary>The index of a GUID in the #GUID heap, explained by the GUID unless
    /// <paramref name="tooltip"/> says otherwise.</summary>
    public GridSchema<TRow> Guid(string name, Func<TRow, GuidHandle> get, Func<TRow, object?>? tooltip = null) =>
        Value(name, r => MetadataTokens.GetHeapOffset(get(r)), "X8",
            tooltip ?? (r => get(r) is { IsNil: false } handle ? _file.Metadata.GetGuid(handle).ToString() : null));

    /// <summary>What is shown under a selected row.</summary>
    public GridSchema<TRow> WithDetails(Func<TRow, MetadataRowDetails?> get)
    {
        Details = r => get((TRow)r);
        return this;
    }

    private GridSchema<TRow> Add<T>(string name, string? format, Func<TRow, int, T> value, Func<TRow, int, object?>? tooltip, bool isToken = false)
    {
        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        var numeric = type.IsEnum || Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal;
        var flags = FlagsSchemaInferer.IsFlags(type) ? FlagsSchemaInferer.For(type) : null;
        Add(new GridColumn(new MetadataColumn(name, isToken, numeric, flags), format,
            (r, rid) => value((TRow)r, rid),
            tooltip == null ? null : (r, rid) => tooltip((TRow)r, rid)));
        return this;
    }
}
