using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class BrowseTreeTests
{
    [Fact]
    public void An_assembly_lists_metadata_references_then_flat_sorted_namespaces()
    {
        var assembly = Trees.TestAssembly;
        var root = assembly.Root;

        Assert.IsType<NodeSubject.Module>(root.Subject);
        Assert.Equal("ZGF.ILSpy.Tests (0.0.0.0, .NETCoreApp, v10.0)", root.Text);
        Assert.IsType<NodeSubject.Metadata>(root.Children[0].Subject);
        Assert.IsType<NodeSubject.References>(root.Children.First(c => c.Subject is not NodeSubject.Metadata).Subject);
        var namespaces = root.Children.Where(c => c.Subject is NodeSubject.Namespace).Select(c => c.Text).ToArray();
        Assert.Equal(["-", "ZGF", "ZGF.ILSpy", "ZGF.ILSpy.Tests", "ZGF.ILSpy.Tests.Fixtures"], namespaces);
        Assert.Contains(root.Child("References").Children, r => r.Subject is NodeSubject.AssemblyReference && r.Text == "System.Runtime");
    }

    [Fact]
    public void Types_have_kinds_labels_and_accessibility()
    {
        var assembly = Trees.TestAssembly;
        var fixtures = assembly.Fixtures();

        Assert.Equal(
            [
                ("Animal", TypeShape.Class), ("Calculator<T>", TypeShape.Class), ("Callback", TypeShape.Delegate), ("Circle", TypeShape.Class),
                ("Enclosure", TypeShape.Struct), ("IFeeds", TypeShape.Interface), ("IShape", TypeShape.Interface), ("Keeper", TypeShape.Class),
                ("Lion", TypeShape.Class), ("NativeImports", TypeShape.Class), ("Parrot", TypeShape.Class), ("Point", TypeShape.Record),
                ("Size", TypeShape.Struct), ("Visits", TypeShape.Class),
            ],
            fixtures.Children.Select(c => (c.Text, c.TypeShape)));
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
            .ToArray();

        Assert.Equal(["Base Types", "Derived Types", "Mode"], children.Take(3).Select(static c => c.Text));
        Assert.IsType<NodeSubject.BaseTypes>(children[0].Subject);
        Assert.IsType<NodeSubject.DerivedTypes>(children[1].Subject);
        Assert.Equal(TypeShape.Enum, children[2].TypeShape);
        Assert.Equal(
            [
                (MemberShape.Constant, "Answer : int"),
                (MemberShape.Field, "_items : List<T>"),
                (MemberShape.Property, "Count : int"),
                (MemberShape.Indexer, "this[int] : T"),
                (MemberShape.Event, "Changed : EventHandler?"),
                (MemberShape.Constructor, "Calculator()"),
                (MemberShape.Method, "Add(T) : void"),
                (MemberShape.Method, "Describe() : string"),
                (MemberShape.Operator, "operator +(Calculator<T>, T) : Calculator<T>"),
                (MemberShape.Method, "TryGet(int, out T?) : bool"),
            ],
            children.Skip(3).Select(static c => (c.MemberShape, c.Text)));
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
        Assert.All(calculator.Child("Count : int").Children, c => Assert.Equal(MemberShape.Method, c.MemberShape));
    }

    [Fact]
    public void Derived_types_list_the_open_types_deriving_directly_and_point_at_them()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var fixtures = assembly.Fixtures();

        var derived = fixtures.Child("IShape").Child("Derived Types").Children;

        var circle = Assert.Single(derived);
        Assert.IsType<NodeSubject.DerivedType>(circle.Subject);
        Assert.Equal("Circle", circle.Text);
        Assert.Equal(NodeAccessibility.Internal, circle.Accessibility);
        Assert.False(circle.HasChildren);
        Assert.Same(fixtures.Child("Circle"), list.FindNode(circle.Destination));
        Assert.DoesNotContain(fixtures.Child("Circle").Children, c => c.Subject is NodeSubject.DerivedTypes);
        Assert.DoesNotContain(fixtures.Child("Size").Children, c => c.Subject is NodeSubject.DerivedTypes);
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

        Assert.IsType<NodeSubject.DerivedType>(argument.Subject);
        Assert.Contains(argument.Children, c => c.Text.EndsWith("ArgumentNullException", StringComparison.Ordinal));
        Assert.Contains(system.Text, exception.Child("Derived Types").Decompile(DecompilationLanguage.CSharp).GetText());
    }

    [Fact]
    public void Base_types_point_at_their_definitions()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var bases = assembly.Fixtures().Child("Circle").Child("Base Types").Children;

        Assert.Equal(["object", "IShape"], bases.Select(b => b.Text));
        Assert.All(bases, b => Assert.IsType<NodeSubject.BaseType>(b.Subject));
        Assert.Same(assembly.Fixtures().Child("IShape"), list.FindNode(bases[1].Destination));
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
    public void A_node_found_by_its_entity_is_the_one_its_parent_lists_whichever_thread_builds_it_first()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var reference = Trees.TestAssembly.Calculator().Child("Add(T) : void").Definition;

        var nodes = Enumerable.Range(0, 16)
            .AsParallel()
            .WithDegreeOfParallelism(8)
            .Select(i => i % 2 == 0 ? list.FindNode(reference) : assembly.Calculator().Child("Add(T) : void"))
            .ToArray();

        Assert.All(nodes, n => Assert.Same(nodes[0], n));
        Assert.Same(assembly.Calculator(), Assert.IsAssignableFrom<BrowseNode>(nodes[0]).Parent);
    }

    [Fact]
    public void A_path_in_the_list_that_holds_its_node_finds_that_very_node()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var add = assembly.Calculator().Child("Add(T) : void");

        Assert.Same(add, list.FindNode(add.Path));
        Assert.Same(add, list.FindListedNode(add.Path));
        Assert.Same(assembly.Root, list.FindNode(assembly.Root.Path));
    }

    [Fact]
    public void Entity_nodes_carry_a_reference_to_themselves()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var add = assembly.Calculator().Child("Add(T) : void");

        Assert.Equal(CodeReferenceKind.Method, add.Definition.Kind);
        Assert.Same(add, list.FindNode(add.Definition));
        Assert.Null(NodeDefinitions.ReferenceOf(assembly.Fixtures().Subject));
    }

    [Fact]
    public void CoreLib_builds_its_tree()
    {
        var list = new AssemblyList();
        var coreLib = list.Open(Trees.CoreLibPath);

        var generic = coreLib.Root.Child("System.Collections.Generic");
        var dictionary = generic.Child("Dictionary<TKey, TValue>");

        Assert.Contains(dictionary.Children, c => c.Text == "TryGetValue(TKey, out TValue) : bool");
        Assert.Contains(dictionary.Children, c => c.Is(TypeShape.Struct) && c.Text == "Enumerator");
        Assert.Contains(coreLib.Root.Children, c => c.Subject is NodeSubject.Resources);
    }
}
