// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata;

/// <summary>The tables ILSpy knows how to list, with the columns it shows for each.</summary>
internal static class MetadataTables
{
    /// <summary>
    /// The schema of table <paramref name="table"/> of <paramref name="file"/>, or null for one
    /// whose rows ILSpy does not list (EnCLog, EnCMap).
    /// </summary>
    public static GridSchema? Create(TableIndex table, MetadataFile file)
    {
        var m = file.Metadata;

        GridSchema<T> Table<T>(Func<IEnumerable<T>> rows) => new(table, file, rows);

        IEnumerable<int> Rids() => Enumerable.Range(1, m.GetTableRowCount(table));

        string? Hex(BlobHandle blob)
        {
            if (blob.IsNil)
                return null;
            var bytes = m.GetBlobContent(blob);
            return bytes.ToHexString(bytes.Length);
        }

        return table switch
        {
            TableIndex.Module => Table<ModuleDefinition>(() => [m.GetModuleDefinition()]).Offset()
                .Value("Generation", r => r.Generation)
                .Name(r => r.Name)
                .Guid("Mvid", r => r.Mvid, r => m.GetGuid(r.Mvid).ToString())
                .Guid("GenerationId", r => r.GenerationId)
                .Guid("BaseGenerationId", r => r.BaseGenerationId),

            TableIndex.TypeRef => Table(() => m.TypeReferences.Select(m.GetTypeReference)).Offset()
                .Handle("ResolutionScope", r => r.ResolutionScope)
                .Name(r => r.Name)
                .String("Namespace", r => r.Namespace),

            TableIndex.TypeDef => Table(() => m.GetTypeDefListColumns().Select(c => (Type: m.GetTypeDefinition(c.Type), c.FieldList, c.MethodList))).Offset()
                .Value("Attributes", r => r.Type.Attributes, "X8")
                .Name(r => r.Type.Name)
                .String("Namespace", r => r.Type.Namespace)
                .Handle("BaseType", r => r.Type.BaseType, r => BaseTypeTooltip(file, r.Type.BaseType))
                .List("FieldList", TableIndex.Field, r => r.FieldList, r => r.Type.GetFields().FirstOrDefault(),
                    "(type has no fields; the stored value is the start of its empty field list: the next type's first field row, or one past the end of the Field table)")
                .List("MethodList", TableIndex.MethodDef, r => r.MethodList, r => r.Type.GetMethods().FirstOrDefault(),
                    "(type has no methods; the stored value is the start of its empty method list: the next type's first method row, or one past the end of the MethodDef table)"),

            TableIndex.Field => Table(() => m.FieldDefinitions.Select(m.GetFieldDefinition)).Offset()
                .Value("Attributes", r => r.Attributes, "X8")
                .Name(r => r.Name)
                .Signature("Signature", r => r.Signature),

            TableIndex.MethodDef => Table(() => m.GetMethodDefParamLists().Select(c => (Method: m.GetMethodDefinition(c.Method), c.ParamList))).Offset()
                .Value("Attributes", r => r.Method.Attributes, "X8")
                .Value("ImplAttributes", r => r.Method.ImplAttributes, "X8")
                .Value("RVA", r => r.Method.RelativeVirtualAddress, "X8")
                .Name(r => r.Method.Name)
                .Signature("Signature", r => r.Method.Signature)
                .List("ParamList", TableIndex.Param, r => r.ParamList, r => r.Method.GetParameters().FirstOrDefault(),
                    "(method has no Param rows; the stored value is the start of its empty parameter list: the next method's first Param row, or one past the end of the Param table)"),

            TableIndex.Param => Table(() => Rids().Select(rid => m.GetParameter(MetadataTokens.ParameterHandle(rid)))).Offset()
                .Value("Attributes", r => r.Attributes, "X8")
                .Value("Sequence", r => r.SequenceNumber)
                .Name(r => r.Name),

            TableIndex.InterfaceImpl => Table(m.GetInterfaceImplRows)
                .Handle("Class", r => r.Class)
                .Handle("Interface", r => r.Interface),

            TableIndex.MemberRef => Table(() => m.MemberReferences.Select(m.GetMemberReference)).Offset()
                .Handle("Parent", r => r.Parent)
                .Name(r => r.Name)
                .Signature("Signature", r => r.Signature),

            TableIndex.Constant => Table(() => Rids().Select(rid => m.GetConstant(MetadataTokens.ConstantHandle(rid)))).Offset()
                .Value("Type", r => r.TypeCode, "X8", r => r.TypeCode.ToString())
                .Handle("Parent", r => r.Parent)
                .Blob("Value", r => r.Value),

            TableIndex.CustomAttribute => Table(() => m.CustomAttributes.Select(m.GetCustomAttribute)).Offset()
                .Handle("Parent", r => r.Parent)
                .Handle("Constructor", r => r.Constructor)
                .Blob("Value", r => r.Value),

            TableIndex.FieldMarshal => Table(m.GetFieldMarshals)
                .Handle("Parent", r => r.Parent)
                .Blob("NativeType", r => r.NativeType),

            TableIndex.DeclSecurity => Table(() => m.DeclarativeSecurityAttributes.Select(m.GetDeclarativeSecurityAttribute)).Offset()
                .Handle("Parent", r => r.Parent)
                .Value("Action", r => r.Action, "X8", r => r.Action.ToString())
                .Blob("PermissionSet", r => r.PermissionSet),

            TableIndex.ClassLayout => Table(m.GetClassLayouts)
                .Handle("Parent", r => r.Parent)
                .Value("PackingSize", r => (ushort)r.PackingSize)
                .Value("ClassSize", r => r.ClassSize),

            TableIndex.FieldLayout => Table(m.GetFieldLayoutRows)
                .Handle("Field", r => r.Field)
                .Value("FieldOffset", r => r.Offset, "X8"),

            TableIndex.StandAloneSig => Table(() => Rids().Select(rid => m.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(rid)))).Offset()
                .Signature("Signature", r => r.Signature),

            TableIndex.EventMap => Table(m.GetEventMaps)
                .Handle("Parent", r => r.Parent)
                .Handle("EventList", r => r.EventList),

            TableIndex.Event => Table(() => m.EventDefinitions.Select(m.GetEventDefinition)).Offset()
                .Value("Attributes", r => r.Attributes, "X8")
                .Name(r => r.Name)
                .Handle("Type", r => r.Type),

            TableIndex.PropertyMap => Table(m.GetPropertyMaps)
                .Handle("Parent", r => r.Parent)
                .Handle("PropertyList", r => r.PropertyList),

            TableIndex.Property => Table(() => m.PropertyDefinitions.Select(m.GetPropertyDefinition)).Offset()
                .Value("Attributes", r => r.Attributes, "X8")
                .Name(r => r.Name)
                .Signature("Signature", r => r.Signature),

            TableIndex.MethodSemantics => Table(m.GetMethodSemantics)
                .Value("Semantics", r => r.Semantics, "X8")
                .Handle("Method", r => r.Method)
                .Handle("Association", r => r.Association),

            TableIndex.MethodImpl => Table(() => Rids().Select(rid => m.GetMethodImplementation(MetadataTokens.MethodImplementationHandle(rid))))
                .Handle("Type", r => r.Type)
                .Handle("MethodDeclaration", r => r.MethodDeclaration)
                .Handle("MethodBody", r => r.MethodBody),

            TableIndex.ModuleRef => Table(() => m.GetModuleReferences().Select(m.GetModuleReference)).Offset()
                .Name(r => r.Name),

            TableIndex.TypeSpec => Table(() => m.GetTypeSpecifications().Select(m.GetTypeSpecification)).Offset()
                .Blob("Signature", r => r.Signature, r =>
                {
                    ITextOutput output = new PlainTextOutput();
                    r.DecodeSignature(new DisassemblerSignatureTypeProvider(file, output), default)(ILNameSyntax.Signature);
                    return output.ToString();
                }),

            TableIndex.ImplMap => Table(m.GetImplMaps)
                .Value("MappingFlags", r => r.MappingFlags, "X8")
                .Handle("MemberForwarded", r => r.MemberForwarded)
                .String("ImportName", r => r.ImportName)
                .Handle("ImportScope", r => r.ImportScope),

            TableIndex.FieldRva => Table(m.GetFieldRvas)
                .Handle("Field", r => r.Field)
                .Value("RVA", r => r.RelativeVirtualAddress, "X8"),

            TableIndex.Assembly => Table<AssemblyDefinition>(() => m.IsAssembly ? [m.GetAssemblyDefinition()] : []).Offset()
                .Value("HashAlgorithm", r => r.HashAlgorithm, "X4", r => r.HashAlgorithm.ToString())
                .Value("Flags", r => r.Flags, "X4")
                .Value("Version", r => r.Version)
                .Name(r => r.Name)
                .String("Culture", r => r.Culture),

            TableIndex.AssemblyRef => Table(() => m.AssemblyReferences.Select(m.GetAssemblyReference)).Offset()
                .Value("Version", r => r.Version)
                .Value("Flags", r => r.Flags, "X8")
                .Blob("PublicKeyOrToken", r => r.PublicKeyOrToken, r => Hex(r.PublicKeyOrToken))
                .Name(r => r.Name)
                .String("Culture", r => r.Culture),

            TableIndex.File => Table(() => m.AssemblyFiles.Select(m.GetAssemblyFile)).Offset()
                .Value("Attributes", r => r.ContainsMetadata ? 1 : 0, "X8", r => r.ContainsMetadata ? "ContainsMetaData" : "ContainsNoMetaData")
                .Name(r => r.Name)
                .Blob("HashValue", r => r.HashValue, r => Hex(r.HashValue)),

            TableIndex.ExportedType => Table(() => m.ExportedTypes.Select(m.GetExportedType)).Offset()
                .Value("Attributes", r => r.Attributes, "X8")
                .Value("TypeDefId", r => r.GetTypeDefinitionId(), "X8")
                .String("TypeName", r => r.Name)
                .String("TypeNamespace", r => r.Namespace)
                .Handle("Implementation", r => r.Implementation),

            TableIndex.ManifestResource => Table(() => m.ManifestResources.Select(m.GetManifestResource)).Offset()
                .Value("Attributes", r => r.Attributes, "X8")
                .Name(r => r.Name)
                .Handle("Implementation", r => r.Implementation),

            TableIndex.NestedClass => Table(m.GetNestedClasses)
                .Handle("NestedClass", r => r.NestedClass)
                .Handle("EnclosingClass", r => r.EnclosingClass),

            TableIndex.GenericParam => Table(() => Rids().Select(rid => m.GetGenericParameter(MetadataTokens.GenericParameterHandle(rid)))).Offset()
                .Value("Number", r => r.Index)
                .Value("Attributes", r => r.Attributes, "X8")
                .Handle("Owner", r => r.Parent)
                .Name(r => r.Name),

            TableIndex.MethodSpec => Table(() => m.GetMethodSpecifications().Select(m.GetMethodSpecification)).Offset()
                .Handle("Method", r => r.Method)
                .Blob("Signature", r => r.Signature, r =>
                {
                    ITextOutput output = new PlainTextOutput();
                    var first = true;
                    foreach (var type in r.DecodeSignature(new DisassemblerSignatureTypeProvider(file, output), default))
                    {
                        if (first)
                            first = false;
                        else
                            output.Write(", ");
                        type(ILNameSyntax.TypeName);
                    }
                    return output.ToString();
                }),

            TableIndex.GenericParamConstraint => Table(() => Rids().Select(rid => m.GetGenericParameterConstraint(MetadataTokens.GenericParameterConstraintHandle(rid)))).Offset()
                .Handle("Owner", r => r.Parameter, r =>
                {
                    ITextOutput output = new PlainTextOutput();
                    var p = m.GetGenericParameter(r.Parameter);
                    output.Write("parameter " + p.Index + (p.Name.IsNil ? "" : " (" + m.GetString(p.Name) + ")") + " of ");
                    p.Parent.WriteTo(file, output, default);
                    return output.ToString();
                })
                .Handle("Type", r => r.Type),

            TableIndex.Document => Table(() => m.Documents.Select(m.GetDocument))
                .Value("Name", r => m.GetString(r.Name), tooltip: r => $"{MetadataTokens.GetHeapOffset(r.Name):X} \"{m.GetString(r.Name)}\"")
                .Guid("HashAlgorithm", r => r.HashAlgorithm, r =>
                {
                    if (r.HashAlgorithm.IsNil)
                        return null;
                    var guid = m.GetGuid(r.HashAlgorithm);
                    if (guid == KnownGuids.HashAlgorithmSHA1)
                        return "SHA1 [ff1816ec-aa5e-4d10-87f7-6f4963833460]";
                    if (guid == KnownGuids.HashAlgorithmSHA256)
                        return "SHA256 [8829d00f-11b8-4213-878b-770e8597ac16]";
                    return "Unknown [" + guid + "]";
                })
                .Blob("Hash", r => r.Hash, r => Hex(r.Hash))
                .Guid("Language", r => r.Language, r =>
                {
                    if (r.Language.IsNil)
                        return null;
                    var guid = m.GetGuid(r.Language);
                    if (guid == KnownGuids.CSharpLanguageGuid)
                        return "Visual C# [3f5162f8-07c6-11d3-9053-00c04fa302a1]";
                    if (guid == KnownGuids.VBLanguageGuid)
                        return "Visual Basic [3a12d0b8-c26c-11d0-b442-00a0244a1dd2]";
                    if (guid == KnownGuids.FSharpLanguageGuid)
                        return "Visual F# [ab4f38c9-b6e6-43ba-be3b-58080b2ccce3]";
                    return "Unknown [" + guid + "]";
                }),

            TableIndex.MethodDebugInformation => Table(() => m.MethodDebugInformation.Select(m.GetMethodDebugInformation))
                .Handle("Document", r => r.Document, r =>
                {
                    if (r.Document.IsNil)
                        return null;
                    var document = m.GetDocument(r.Document);
                    return $"{MetadataTokens.GetHeapOffset(document.Name):X} \"{m.GetString(document.Name)}\"";
                })
                .Blob("SequencePoints", r => r.SequencePointsBlob, r =>
                {
                    if (r.SequencePointsBlob.IsNil)
                        return null;
                    var sb = new StringBuilder();
                    foreach (var p in r.GetSequencePoints())
                        sb.AppendLine($"document='{MetadataTokens.GetToken(p.Document):X8}', offset={p.Offset}, start={p.StartLine};{p.StartColumn}, end={p.EndLine};{p.EndColumn}, hidden={p.IsHidden}");
                    return sb.ToString().TrimEnd();
                })
                .Handle("LocalSignature", r => r.LocalSignature, r =>
                {
                    if (r.LocalSignature.IsNil)
                        return null;
                    ITextOutput output = new PlainTextOutput();
                    var context = new MetadataGenericContext(default(TypeDefinitionHandle), m);
                    var signatureDecoder = new DisassemblerSignatureTypeProvider(file, output);
                    var index = 0;
                    foreach (var item in m.GetStandaloneSignature(r.LocalSignature).DecodeLocalSignature(signatureDecoder, context))
                    {
                        if (index > 0)
                            output.WriteLine();
                        output.Write("[{0}] ", index);
                        item(ILNameSyntax.Signature);
                        index++;
                    }
                    return output.ToString();
                }),

            TableIndex.LocalScope => Table(() => m.LocalScopes.Select(m.GetLocalScope))
                .Handle("Method", r => r.Method)
                .Handle("ImportScope", r => r.ImportScope, NoTooltip)
                .Handle("VariableList", r => r.GetLocalVariables().FirstOrDefault(), NoTooltip)
                .Handle("ConstantList", r => r.GetLocalConstants().FirstOrDefault(), NoTooltip)
                .Value("StartOffset", r => r.StartOffset)
                .Value("Length", r => r.Length),

            TableIndex.LocalVariable => Table(() => m.LocalVariables.Select(m.GetLocalVariable))
                .Value("Attributes", r => r.Attributes, "X8")
                .Value("Index", r => r.Index)
                .Name(r => r.Name),

            TableIndex.LocalConstant => Table(() => m.LocalConstants.Select(m.GetLocalConstant))
                .Name(r => r.Name)
                .Blob("Signature", r => r.Signature),

            TableIndex.ImportScope => Table(() => m.ImportScopes.Select(m.GetImportScope))
                .Handle("Parent", r => r.Parent, NoTooltip)
                .Blob("Imports", r => r.ImportsBlob),

            TableIndex.StateMachineMethod => Table(() => StateMachineMethods(m))
                .Handle("MoveNextMethod", r => r.MoveNext)
                .Handle("KickoffMethod", r => r.Kickoff),

            TableIndex.CustomDebugInformation => CustomDebugInformationTable.Columns(Table(() => m.CustomDebugInformation.Select(m.GetCustomDebugInformation)), file),

            TableIndex.FieldPtr or TableIndex.MethodPtr or TableIndex.ParamPtr or TableIndex.EventPtr or TableIndex.PropertyPtr => Table(() => m.GetPtrRows(table))
                .Handle("Handle", r => r),

            _ => null,
        };
    }

    private static object? NoTooltip<T>(T row) => null;

    private static string? BaseTypeTooltip(MetadataFile file, EntityHandle baseType)
    {
        if (baseType.IsNil)
            return null;
        var output = new PlainTextOutput();
        var provider = new DisassemblerSignatureTypeProvider(file, output);
        switch (baseType.Kind)
        {
            case HandleKind.TypeDefinition:
                provider.GetTypeFromDefinition(file.Metadata, (TypeDefinitionHandle)baseType, 0)(ILNameSyntax.Signature);
                return output.ToString();
            case HandleKind.TypeReference:
                provider.GetTypeFromReference(file.Metadata, (TypeReferenceHandle)baseType, 0)(ILNameSyntax.Signature);
                return output.ToString();
            case HandleKind.TypeSpecification:
                provider.GetTypeFromSpecification(file.Metadata, new MetadataGenericContext(default(TypeDefinitionHandle), file.Metadata), (TypeSpecificationHandle)baseType, 0)(ILNameSyntax.Signature);
                return output.ToString();
            default:
                return null;
        }
    }

    /// <summary>The StateMachineMethod rows, read from the table: System.Reflection.Metadata has no
    /// reader for them. A MethodDef index is two bytes or four, as half the row is.</summary>
    private static IEnumerable<(MethodDefinitionHandle MoveNext, MethodDefinitionHandle Kickoff)> StateMachineMethods(MetadataReader metadata)
    {
        var length = metadata.GetTableRowCount(TableIndex.StateMachineMethod);
        var reader = metadata.AsBlobReader();
        reader.Offset = metadata.GetTableMetadataOffset(TableIndex.StateMachineMethod);
        var methodDefSize = metadata.GetTableRowSize(TableIndex.StateMachineMethod) / 2;
        var rows = new List<(MethodDefinitionHandle, MethodDefinitionHandle)>(length);
        for (var rid = 1; rid <= length; rid++)
        {
            var moveNext = MetadataTokens.MethodDefinitionHandle(methodDefSize == 2 ? reader.ReadUInt16() : reader.ReadInt32());
            var kickoff = MetadataTokens.MethodDefinitionHandle(methodDefSize == 2 ? reader.ReadUInt16() : reader.ReadInt32());
            rows.Add((moveNext, kickoff));
        }
        return rows;
    }
}
