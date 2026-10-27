using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class BrowseTreeTests
{
    [Fact]
    public void An_assembly_lists_metadata_references_then_flat_sorted_namespaces()
    {
        var assembly = Trees.TestAssembly;
        var root = assembly.Root;

        Assert.Equal(NodeKind.Assembly, root.Kind);
        Assert.Equal("ZGF.ILSpy.Tests (0.0.0.0, .NETCoreApp, v10.0)", root.Text);
        Assert.Equal(NodeKind.Metadata, root.Children[0].Kind);
        Assert.Equal(NodeKind.ReferencesFolder, root.Children.First(c => c.Kind != NodeKind.Metadata).Kind);
        var namespaces = root.Children.Where(c => c.Kind == NodeKind.Namespace).Select(c => c.Text).ToArray();
        Assert.Equal(["-", "ZGF", "ZGF.ILSpy", "ZGF.ILSpy.Tests", "ZGF.ILSpy.Tests.Fixtures"], namespaces);
        Assert.Contains(root.Child("References").Children, r => r.Kind == NodeKind.AssemblyReference && r.Text == "System.Runtime");
    }

    [Fact]
    public void Types_have_kinds_labels_and_accessibility()
    {
        var assembly = Trees.TestAssembly;
        var fixtures = assembly.Fixtures();

        Assert.Equal(
            [
                ("Calculator<T>", NodeKind.Class), ("Callback", NodeKind.Delegate), ("Circle", NodeKind.Class), ("IShape", NodeKind.Interface),
                ("NativeImports", NodeKind.Class), ("Point", NodeKind.Record), ("Size", NodeKind.Struct),
            ],
            fixtures.Children.Select(c => (c.Text, c.Kind)));
        var circle = fixtures.Child("Circle");
        Assert.Equal(NodeAccessibility.Internal, circle.Accessibility);
        Assert.False(circle.IsPublicApi);
        Assert.False(circle.Child("Area : double").IsPublicApi);
        Assert.Equal(NodeAccessibility.Public, circle.Child("Area : double").Accessibility);
        Assert.Equal("ZGF.ILSpy.Tests.Fixtures.Calculator<T>", fixtures.Child("Calculator<T>").FullName);
    }

    [Fact]
    public void Type_members_come_in_ILSpy_order_with_signatures()
    {
        var assembly = Trees.TestAssembly;

        var children = assembly.Calculator().Children
            .Where(c => !c.IsCompilerGenerated)
            .Select(c => (c.Kind, c.Text))
            .ToArray();

        Assert.Equal(
            [
                (NodeKind.BaseTypes, "Base Types"),
                (NodeKind.DerivedTypes, "Derived Types"),
                (NodeKind.Enum, "Mode"),
                (NodeKind.Constant, "Answer : int"),
                (NodeKind.Field, "_items : List<T>"),
                (NodeKind.Property, "Count : int"),
                (NodeKind.Indexer, "this[int] : T"),
                (NodeKind.Event, "Changed : EventHandler?"),
                (NodeKind.Constructor, "Calculator()"),
                (NodeKind.Method, "Add(T) : void"),
                (NodeKind.Method, "Describe() : string"),
                (NodeKind.Operator, "operator +(Calculator<T>, T) : Calculator<T>"),
                (NodeKind.Method, "TryGet(int, out T?) : bool"),
            ],
            children);
    }

    [Fact]
    public void Enum_members_keep_declaration_order_without_the_value_field()
    {
        var assembly = Trees.TestAssembly;

        var mode = assembly.Calculator().Child("Mode");

        Assert.Equal(["Base Types", "Fast : Mode", "Slow : Mode", "Precise : Mode"], mode.Children.Select(c => c.Text));
    }

    [Fact]
    public void Properties_and_events_hold_their_accessors()
    {
        var assembly = Trees.TestAssembly;
        var calculator = assembly.Calculator();

        Assert.Equal(["get_Count"], calculator.Child("Count : int").Children.Select(c => c.Text));
        Assert.Equal(["add_Changed", "remove_Changed"], calculator.Child("Changed : EventHandler?").Children.Select(c => c.Text));
        Assert.All(calculator.Child("Count : int").Children, c => Assert.Equal(NodeKind.Method, c.Kind));
    }

    [Fact]
    public void Derived_types_list_the_open_types_deriving_directly_and_point_at_them()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var fixtures = assembly.Fixtures();

        var derived = fixtures.Child("IShape").Child("Derived Types").Children;

        var circle = Assert.Single(derived);
        Assert.Equal((NodeKind.DerivedType, "Circle"), (circle.Kind, circle.Text));
        Assert.Equal(NodeAccessibility.Internal, circle.Accessibility);
        Assert.False(circle.HasChildren);
        Assert.Same(fixtures.Child("Circle"), list.FindNode(circle.Target!));
        Assert.DoesNotContain(fixtures.Child("Circle").Children, c => c.Kind == NodeKind.DerivedTypes);
        Assert.DoesNotContain(fixtures.Child("Size").Children, c => c.Kind == NodeKind.DerivedTypes);
        Assert.Empty(assembly.Calculator().Child("Derived Types").Children);
        Assert.Equal("Circle", ZGF.ILSpy.Core.Analysis.Analyzers.Of(circle)!.Label);
    }

    [Fact]
    public void A_derived_type_expands_to_the_types_deriving_from_it_in_turn()
    {
        var list = new AssemblyList();
        var corelib = list.Open(Trees.CoreLibPath);
        var exception = corelib.Root.Child("System").Child("Exception");

        var system = exception.Child("Derived Types").Children.Single(c => c.Text.EndsWith("SystemException", StringComparison.Ordinal));
        var argument = system.Children.Single(c => c.Text.EndsWith("ArgumentException", StringComparison.Ordinal));

        Assert.Equal(NodeKind.DerivedType, argument.Kind);
        Assert.Contains(argument.Children, c => c.Text.EndsWith("ArgumentNullException", StringComparison.Ordinal));
        Assert.Contains(system.Text, exception.Child("Derived Types").Decompile(DecompilationLanguage.CSharp).GetText());
    }

    [Fact]
    public void Base_types_point_at_their_definitions()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var bases = assembly.Fixtures().Child("Circle").Child("Base Types").Children;

        Assert.Equal(["object", "IShape"], bases.Select(b => b.Text));
        Assert.All(bases, b => Assert.Equal(NodeKind.BaseType, b.Kind));
        Assert.Same(assembly.Fixtures().Child("IShape"), list.FindNode(bases[1].Target!));
    }

    [Fact]
    public void HasChildren_answers_without_building_children_and_agrees_after()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var calculator = assembly.Calculator();

        var add = calculator.Child("Add(T) : void");
        var size = assembly.Fixtures().Child("Size");

        Assert.False(add.HasChildren);
        Assert.True(size.HasChildren);
        Assert.NotEmpty(size.Children);
        Assert.True(calculator.Child("Count : int").HasChildren);
    }

    [Fact]
    public void Nodes_keep_their_identity_across_reads_and_threads()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var nodes = Enumerable.Range(0, 16).AsParallel().Select(_ => assembly.Calculator().Child("Add(T) : void")).ToArray();

        Assert.All(nodes, n => Assert.Same(nodes[0], n));
        Assert.Same(assembly.Calculator(), nodes[0].Parent);
        Assert.Same(assembly, nodes[0].Assembly);
    }

    [Fact]
    public void Entity_nodes_carry_a_reference_to_themselves()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var add = assembly.Calculator().Child("Add(T) : void");

        Assert.NotNull(add.Reference);
        Assert.Equal(CodeReferenceKind.Method, add.Reference!.Kind);
        Assert.Same(add, list.FindNode(add.Reference));
        Assert.Null(assembly.Fixtures().Reference);
    }

    [Fact]
    public void CoreLib_builds_its_tree()
    {
        var list = new AssemblyList();
        var coreLib = list.Open(Trees.CoreLibPath);

        var generic = coreLib.Root.Child("System.Collections.Generic");
        var dictionary = generic.Child("Dictionary<TKey, TValue>");

        Assert.Contains(dictionary.Children, c => c.Text == "TryGetValue(TKey, out TValue) : bool");
        Assert.Contains(dictionary.Children, c => c.Kind == NodeKind.Struct && c.Text == "Enumerator");
        Assert.Contains(coreLib.Root.Children, c => c.Kind == NodeKind.ResourcesFolder);
    }
}
