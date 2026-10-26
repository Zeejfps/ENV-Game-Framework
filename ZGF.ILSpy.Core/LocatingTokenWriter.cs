using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace ZGF.ILSpy.Core;

/// <summary>Passes C# on to the writers it decorates, telling the decompiler where in
/// <see cref="DocumentBuilder"/> each token lands, so that it can record where each node of the syntax
/// tree was written.</summary>
internal sealed class LocatingTokenWriter(TokenWriter decorated, DocumentBuilder builder) : DecoratingTokenWriter(decorated), ILocatable
{
    public TextLocation Location => builder.Location;

    public int Length => 0;
}
