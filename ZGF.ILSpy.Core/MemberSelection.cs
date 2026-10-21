// Adapted from ILSpy's CSharpLanguage (MIT License, Copyright (c) AlphaSierraPapa for the SharpDevelop Team).

using System.Reflection.Metadata;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>
/// How ILSpy shows a constructor or a field on its own: the type's fields, events, properties and
/// constructors of the same staticness are decompiled together, so initializers moved into a
/// constructor come back out, and then everything but the one asked for is removed.
/// </summary>
internal static class MemberSelection
{
    public static List<EntityHandle> FieldsAndConstructors(ITypeDefinition type, bool isStatic)
    {
        var members = new List<EntityHandle>();
        foreach (var field in type.Fields)
        {
            if (!field.MetadataToken.IsNil && field.IsStatic == isStatic)
                members.Add(field.MetadataToken);
        }
        foreach (var e in type.Events)
        {
            if (!e.MetadataToken.IsNil && e.IsStatic == isStatic)
                members.Add(e.MetadataToken);
        }
        foreach (var p in type.Properties)
        {
            if (!p.MetadataToken.IsNil && p.IsStatic == isStatic && !p.IsIndexer)
                members.Add(p.MetadataToken);
        }
        foreach (var ctor in type.Methods)
        {
            if (!ctor.MetadataToken.IsNil && ctor.IsConstructor && ctor.IsStatic == isStatic)
                members.Add(ctor.MetadataToken);
        }
        return members;
    }

    /// <summary>Keeps <paramref name="ctor"/> and the members whose initializers it runs.</summary>
    public sealed class Constructor(IMethod ctor) : IAstTransform
    {
        private readonly HashSet<ISymbol?> _removed = new();

        public void Run(AstNode rootNode, TransformContext context)
        {
            ConstructorDeclaration? kept = null;
            foreach (var node in rootNode.Children)
            {
                switch (node)
                {
                    case ConstructorDeclaration cd:
                        if (cd.GetSymbol() == ctor)
                            kept = cd;
                        else
                            Remove(cd);
                        break;
                    case FieldDeclaration fd when fd.Variables.All(static v => v.Initializer is null):
                        Remove(fd);
                        break;
                    case EventDeclaration ed when ed.Variables.All(static v => v.Initializer is null):
                        Remove(ed);
                        break;
                    case PropertyDeclaration { Initializer: null } pd:
                        Remove(pd);
                        break;
                    case CustomEventDeclaration or IndexerDeclaration:
                        Remove(node);
                        break;
                }
            }
            if (kept?.Initializer?.ConstructorInitializerType == ConstructorInitializerType.This)
            {
                foreach (var node in rootNode.Children)
                {
                    if (node is not ConstructorDeclaration)
                        Remove(node);
                }
            }
            foreach (var node in rootNode.Children)
            {
                if (node is Comment && _removed.Contains(node.GetSymbol()))
                    node.Remove();
            }
        }

        private void Remove(AstNode node)
        {
            node.Remove();
            _removed.Add(node.GetSymbol());
        }
    }

    /// <summary>Keeps only <paramref name="field"/>.</summary>
    public sealed class Field(IField field) : IAstTransform
    {
        public void Run(AstNode rootNode, TransformContext context)
        {
            foreach (var node in rootNode.Children)
            {
                if (node is EntityDeclaration or Comment && node.GetSymbol() != field)
                    node.Remove();
            }
        }
    }
}
