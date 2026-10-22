// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// Common parent for the tables listed under "Tables". Each subclass corresponds to
	/// one CLI <see cref="TableIndex"/> and surfaces its rows.
	/// </summary>
	internal abstract class MetadataTable
	{
		protected readonly MetadataFile metadataFile;

		public TableIndex Kind { get; }

		public int RowCount => metadataFile.Metadata.GetTableRowCount(Kind);

		protected MetadataTable(TableIndex kind, MetadataFile metadataFile)
		{
			Kind = kind;
			this.metadataFile = metadataFile ?? throw new ArgumentNullException(nameof(metadataFile));
		}

		public string Text => $"{(byte)Kind:X2} {Kind} ({RowCount})";

		/// <summary>The type of the rows <see cref="LoadRows"/> gives.</summary>
		public abstract Type EntryType { get; }

		/// <summary>Every row, in table order, loaded once.</summary>
		public abstract IReadOnlyList<object> LoadRows();

		/// <summary>
		/// Byte offset of row <paramref name="rid"/> (1-based) in <paramref name="table"/>, relative to
		/// the start of the PE file. Every typed row viewmodel exposes this from its <c>Offset</c>
		/// property so the hex view can jump to the raw bytes.
		/// </summary>
		protected static int GetRowOffset(MetadataFile file, TableIndex table, int rid)
			=> GetRowOffset(file.Metadata, file.MetadataOffset, table, rid);

		/// <summary>
		/// Overload for the few entries that hold a raw <see cref="MetadataReader"/> + offset rather
		/// than a <see cref="MetadataFile"/>.
		/// </summary>
		protected static int GetRowOffset(MetadataReader metadata, int metadataOffset, TableIndex table, int rid)
			=> metadataOffset + metadata.GetTableMetadataOffset(table) + metadata.GetTableRowSize(table) * (rid - 1);

		/// <summary>
		/// Builds (and caches into <paramref name="tooltip"/>) a human-readable description of the
		/// entity a token column points at, e.g. <c>(AssemblyReference) System.Runtime, ...</c>.
		/// Entries expose this from their <c>{Column}Tooltip</c> properties so hovering a token cell
		/// shows what it refers to. Returns <see langword="null"/> for a nil handle.
		/// </summary>
		protected static string? GenerateTooltip(ref string? tooltip, MetadataFile module, EntityHandle handle)
		{
			if (tooltip == null)
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
						ModuleReference moduleReference = metadata.GetModuleReference((ModuleReferenceHandle)handle);
						output.Write(metadata.GetString(moduleReference.Name));
						break;
					case HandleKind.AssemblyReference:
						var asmRef = new ICSharpCode.Decompiler.Metadata.AssemblyReference(module, (AssemblyReferenceHandle)handle);
						output.Write(asmRef.ToString());
						break;
					case HandleKind.Parameter:
						var param = metadata.GetParameter((ParameterHandle)handle);
						output.Write(param.SequenceNumber + " - " + metadata.GetString(param.Name));
						break;
					case HandleKind.EventDefinition:
						var @event = metadata.GetEventDefinition((EventDefinitionHandle)handle);
						output.Write(metadata.GetString(@event.Name));
						break;
					case HandleKind.PropertyDefinition:
						var prop = metadata.GetPropertyDefinition((PropertyDefinitionHandle)handle);
						output.Write(metadata.GetString(prop.Name));
						break;
					case HandleKind.AssemblyDefinition:
						var ad = metadata.GetAssemblyDefinition();
						output.Write(metadata.GetString(ad.Name));
						output.Write(" (this assembly)");
						break;
					case HandleKind.AssemblyFile:
						var af = metadata.GetAssemblyFile((AssemblyFileHandle)handle);
						output.Write(metadata.GetString(af.Name));
						break;
					case HandleKind.GenericParameter:
						var gp = metadata.GetGenericParameter((GenericParameterHandle)handle);
						output.Write(metadata.GetString(gp.Name));
						break;
					case HandleKind.ManifestResource:
						var mfr = metadata.GetManifestResource((ManifestResourceHandle)handle);
						output.Write(metadata.GetString(mfr.Name));
						break;
					case HandleKind.Document:
						var doc = metadata.GetDocument((DocumentHandle)handle);
						output.Write(metadata.GetString(doc.Name));
						break;
					default:
						handle.WriteTo(module, output, context);
						break;
				}
				tooltip = "(" + handle.Kind + ") " + output.ToString();
			}
			return tooltip;
		}
	}

	/// <summary>
	/// Typed companion: holds the row materialiser. Rows are loaded lazily and cached so
	/// repeated activations don't re-walk the metadata.
	/// </summary>
	internal abstract class MetadataTable<TEntry> : MetadataTable
		where TEntry : class
	{
		IReadOnlyList<TEntry>? cached;

		protected MetadataTable(TableIndex kind, MetadataFile metadataFile)
			: base(kind, metadataFile)
		{
		}

		public override Type EntryType => typeof(TEntry);

		public override IReadOnlyList<object> LoadRows()
		{
			lock (this)
				return cached ??= LoadTable();
		}

		protected abstract IReadOnlyList<TEntry> LoadTable();
	}

	/// <summary>The tables ILSpy knows how to list.</summary>
	internal static class MetadataTables
	{
		/// <summary>
		/// The table <paramref name="table"/> of <paramref name="metadataFile"/>, or null for one
		/// whose rows ILSpy does not list (EnCLog, EnCMap).
		/// </summary>
		public static MetadataTable? Create(TableIndex table, MetadataFile metadataFile)
			=> table switch {
				TableIndex.Module => new ModuleTable(metadataFile),
				TableIndex.TypeRef => new TypeRefTable(metadataFile),
				TableIndex.TypeDef => new TypeDefTable(metadataFile),
				TableIndex.Field => new FieldTable(metadataFile),
				TableIndex.MethodDef => new MethodTable(metadataFile),
				TableIndex.Param => new ParamTable(metadataFile),
				TableIndex.MemberRef => new MemberRefTable(metadataFile),
				TableIndex.Constant => new ConstantTable(metadataFile),
				TableIndex.CustomAttribute => new CustomAttributeTable(metadataFile),
				TableIndex.Event => new EventTable(metadataFile),
				TableIndex.Property => new PropertyTable(metadataFile),
				TableIndex.ModuleRef => new ModuleRefTable(metadataFile),
				TableIndex.TypeSpec => new TypeSpecTable(metadataFile),
				TableIndex.Assembly => new AssemblyTable(metadataFile),
				TableIndex.AssemblyRef => new AssemblyRefTable(metadataFile),
				TableIndex.ExportedType => new ExportedTypeTable(metadataFile),
				TableIndex.ManifestResource => new ManifestResourceTable(metadataFile),
				TableIndex.GenericParam => new GenericParamTable(metadataFile),
				TableIndex.MethodSpec => new MethodSpecTable(metadataFile),
				TableIndex.GenericParamConstraint => new GenericParamConstraintTable(metadataFile),
				TableIndex.StandAloneSig => new StandAloneSigTable(metadataFile),
				TableIndex.DeclSecurity => new DeclSecurityTable(metadataFile),
				TableIndex.File => new FileTable(metadataFile),
				TableIndex.Document => new DocumentTable(metadataFile),
				TableIndex.MethodDebugInformation => new MethodDebugInformationTable(metadataFile),
				TableIndex.LocalScope => new LocalScopeTable(metadataFile),
				TableIndex.LocalVariable => new LocalVariableTable(metadataFile),
				TableIndex.LocalConstant => new LocalConstantTable(metadataFile),
				TableIndex.ImportScope => new ImportScopeTable(metadataFile),
				TableIndex.StateMachineMethod => new StateMachineMethodTable(metadataFile),
				TableIndex.CustomDebugInformation => new CustomDebugInformationTable(metadataFile),
				TableIndex.MethodImpl => new MethodImplTable(metadataFile),
				TableIndex.MethodSemantics => new MethodSemanticsTable(metadataFile),
				TableIndex.ClassLayout => new ClassLayoutTable(metadataFile),
				TableIndex.FieldLayout => new FieldLayoutTable(metadataFile),
				TableIndex.FieldRva => new FieldRVATable(metadataFile),
				TableIndex.NestedClass => new NestedClassTable(metadataFile),
				TableIndex.EventMap => new EventMapTable(metadataFile),
				TableIndex.PropertyMap => new PropertyMapTable(metadataFile),
				TableIndex.InterfaceImpl => new InterfaceImplTable(metadataFile),
				TableIndex.FieldMarshal => new FieldMarshalTable(metadataFile),
				TableIndex.ImplMap => new ImplMapTable(metadataFile),
				TableIndex.FieldPtr or TableIndex.MethodPtr or TableIndex.ParamPtr
					or TableIndex.EventPtr or TableIndex.PropertyPtr => new PtrTable(table, metadataFile),
				_ => null,
			};
	}
}
