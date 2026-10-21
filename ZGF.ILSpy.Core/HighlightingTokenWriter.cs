using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

internal sealed class HighlightingTokenWriter : DecoratingTokenWriter
{
    private readonly DocumentBuilder _builder;
    private readonly Stack<AstNode> _nodes = new();

    public HighlightingTokenWriter(TokenWriter decorated, DocumentBuilder builder) : base(decorated)
    {
        _builder = builder;
    }

    public override void StartNode(AstNode node)
    {
        _nodes.Push(node);
        base.StartNode(node);
        foreach (var instruction in node.Annotations.OfType<ILInstruction>())
        {
            if (instruction.ILRangeIsEmpty || instruction.Parent is null || instruction is BlockContainer or Block) continue;
            var function = instruction.Parent.Ancestors.OfType<ILFunction>().FirstOrDefault();
            if (function is { Kind: ILFunctionKind.TopLevelFunction, Method: { } method })
                _builder.MarkStatement(method, instruction.StartILOffset);
        }
    }

    public override void EndNode(AstNode node)
    {
        base.EndNode(node);
        _nodes.Pop();
    }

    public override void WriteKeyword(Role role, string keyword)
    {
        _builder.Kind = SpanKind.Keyword;
        base.WriteKeyword(role, keyword);
        _builder.Kind = null;
    }

    public override void WritePrimitiveType(string type)
    {
        _builder.Kind = SpanKind.Keyword;
        base.WritePrimitiveType(type);
        _builder.Kind = null;
    }

    public override void WritePrimitiveValue(object value, LiteralFormat format = LiteralFormat.None)
    {
        _builder.Kind = value switch
        {
            null or bool => SpanKind.Keyword,
            string or char => SpanKind.String,
            _ => SpanKind.Number,
        };
        base.WritePrimitiveValue(value, format);
        _builder.Kind = null;
    }

    public override void WriteInterpolatedText(string text)
    {
        _builder.Kind = SpanKind.String;
        base.WriteInterpolatedText(text);
        _builder.Kind = null;
    }

    public override void WriteToken(Role role, string token)
    {
        _builder.Kind = IsStringDelimiter(token) ? SpanKind.String : SpanKind.Plain;
        base.WriteToken(role, token);
        _builder.Kind = null;
    }

    public override void WriteComment(CommentType commentType, string content)
    {
        _builder.Kind = SpanKind.Comment;
        base.WriteComment(commentType, content);
        _builder.Kind = null;
    }

    public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
    {
        _builder.Kind = SpanKind.Preprocessor;
        base.WritePreProcessorDirective(type, argument);
        _builder.Kind = null;
    }

    public override void WriteIdentifier(Identifier identifier)
    {
        _builder.Kind = Classify(identifier);
        base.WriteIdentifier(identifier);
        _builder.Kind = null;
    }

    private bool IsStringDelimiter(string token) =>
        token.StartsWith('$') || (token == "\"" && _nodes.TryPeek(out var node) && node is InterpolatedStringExpression);

    private SpanKind? Classify(Identifier identifier)
    {
        if (!_nodes.TryPeek(out var node)) return null;
        if (node is Identifier) node = node.Parent;
        if (node is null) return null;

        switch (node.GetSymbol())
        {
            case IMethod { IsConstructor: true } or IMethod { IsDestructor: true }:
                return SpanKind.Type;
            case IMethod:
                return SpanKind.Method;
            case IField:
                return SpanKind.Field;
            case IProperty:
                return SpanKind.Property;
            case IEvent:
                return SpanKind.Event;
            case IParameter:
                return SpanKind.Parameter;
            case IVariable:
                return SpanKind.Local;
            case INamespace:
                return SpanKind.Namespace;
            case IType:
                return SpanKind.Type;
        }

        switch (node.GetResolveResult())
        {
            case NamespaceResolveResult:
                return SpanKind.Namespace;
            case ILVariableResolveResult { Variable.Kind: VariableKind.Parameter }:
                return SpanKind.Parameter;
            case ILVariableResolveResult:
                return SpanKind.Local;
            case TypeResolveResult:
                return SpanKind.Type;
        }

        for (var ancestor = node; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is UsingDeclaration or NamespaceDeclaration) return SpanKind.Namespace;
            if (ancestor is EntityDeclaration or Statement) break;
        }

        return node switch
        {
            TypeParameterDeclaration or TypeDeclaration or DelegateDeclaration => SpanKind.Type,
            ParameterDeclaration => SpanKind.Parameter,
            SingleVariableDesignation or CatchClause or ForeachStatement => SpanKind.Local,
            _ => null,
        };
    }
}
