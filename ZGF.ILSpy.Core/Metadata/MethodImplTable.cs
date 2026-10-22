// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// View of the MethodImpl table — explicit interface implementations and method-body
	/// overrides where the declared signature differs from the impl signature. Each row
	/// pairs a Type, a MethodDeclaration (the override target), and a MethodBody (the impl).
	/// </summary>
	internal sealed class MethodImplTable : MetadataTable<MethodImplTable.MethodImplEntry>
	{
		public MethodImplTable(MetadataFile metadataFile)
			: base(TableIndex.MethodImpl, metadataFile)
		{
		}

		protected override IReadOnlyList<MethodImplEntry> LoadTable()
		{
			var list = new List<MethodImplEntry>();
			for (int row = 1; row <= metadataFile.Metadata.GetTableRowCount(TableIndex.MethodImpl); row++)
				list.Add(new MethodImplEntry(metadataFile, MetadataTokens.MethodImplementationHandle(row)));
			return list;
		}

		public sealed class MethodImplEntry
		{
			readonly MetadataFile metadataFile;
			readonly MethodImplementationHandle handle;
			readonly MethodImplementation methodImpl;

			public int RID => MetadataTokens.GetToken(handle) & 0xFFFFFF;

			[ColumnInfo("X8")]
			public int Token => MetadataTokens.GetToken(handle);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int Type => MetadataTokens.GetToken(methodImpl.Type);

			string? typeTooltip;
			public string? TypeTooltip => GenerateTooltip(ref typeTooltip, metadataFile, methodImpl.Type);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int MethodDeclaration => MetadataTokens.GetToken(methodImpl.MethodDeclaration);

			string? methodDeclarationTooltip;
			public string? MethodDeclarationTooltip => GenerateTooltip(ref methodDeclarationTooltip, metadataFile, methodImpl.MethodDeclaration);

			[ColumnInfo("X8", Kind = ColumnKind.Token)]
			public int MethodBody => MetadataTokens.GetToken(methodImpl.MethodBody);

			string? methodBodyTooltip;
			public string? MethodBodyTooltip => GenerateTooltip(ref methodBodyTooltip, metadataFile, methodImpl.MethodBody);

			public MethodImplEntry(MetadataFile metadataFile, MethodImplementationHandle handle)
			{
				this.metadataFile = metadataFile;
				this.handle = handle;
				methodImpl = metadataFile.Metadata.GetMethodImplementation(handle);
			}
		}
	}
}
