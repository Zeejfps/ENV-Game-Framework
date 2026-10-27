using ZGF.Geometry;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;

namespace ZGF.ILSpy.Tests;

public sealed class TypeLayoutTests
{
    private const string ZooNamespace = Trees.FixturesNamespace;
    private const float Near = 1.5f;

    private static CodeReference Zoo(string type) => Trees.TestAssembly.Root.Child(ZooNamespace, type).Reference!;

    private static readonly CodeReference Animal = Zoo("Animal");
    private static readonly CodeReference Lion = Zoo("Lion");
    private static readonly CodeReference Parrot = Zoo("Parrot");
    private static readonly CodeReference Keeper = Zoo("Keeper");

    private static readonly TypeLink LionIsAnimal = new(Lion, Animal, TypeLinkKind.Inherits);
    private static readonly TypeLink ParrotIsAnimal = new(Parrot, Animal, TypeLinkKind.Inherits);
    private static readonly TypeLink KeeperHoldsAnimals = new(Keeper, Animal, TypeLinkKind.HoldsMany, "Animals");

    private static DiagramMeasures Zoo4()
    {
        MeasuredCard[] cards =
        [
            new(Animal, 160f, 120f),
            new(Lion, 140f, 80f),
            new(Parrot, 120f, 60f),
            new(Keeper, 150f, 90f),
        ];
        MeasuredLink[] links =
        [
            new(LionIsAnimal, 0f, 0f),
            new(ParrotIsAnimal, 0f, 0f),
            new(KeeperHoldsAnimals, 60f, 16f),
        ];
        return new DiagramMeasures(cards, links);
    }

    private static (PointF TopLeft, MeasuredCard Card) Placed(TypeLayout layout, DiagramMeasures measures, CodeReference type)
    {
        var card = measures.Cards.Single(c => c.Type == type);
        return (layout.Places[type], card);
    }

    private static bool IsOnBorder(PointF point, PointF topLeft, MeasuredCard card)
    {
        var inside = point.X > topLeft.X - Near && point.X < topLeft.X + card.Width + Near && point.Y > topLeft.Y - Near && point.Y < topLeft.Y + card.Height + Near;
        var nearSide = MathF.Abs(point.X - topLeft.X) < Near || MathF.Abs(point.X - topLeft.X - card.Width) < Near;
        var nearEdge = MathF.Abs(point.Y - topLeft.Y) < Near || MathF.Abs(point.Y - topLeft.Y - card.Height) < Near;
        return inside && (nearSide || nearEdge);
    }

    [Fact]
    public void Every_card_is_placed_and_the_types_derived_from_sit_above_those_deriving_from_them()
    {
        var measures = Zoo4();

        var layout = TypeLayout.Arrange(measures);

        Assert.Equal(4, layout.Places.Count);
        var animal = Placed(layout, measures, Animal);
        var lion = Placed(layout, measures, Lion);
        Assert.True(animal.TopLeft.Y + animal.Card.Height <= lion.TopLeft.Y);
    }

    [Fact]
    public void No_two_cards_overlap_and_the_layout_holds_them_all_from_its_top_left()
    {
        var measures = Zoo4();

        var layout = TypeLayout.Arrange(measures);

        var boxes = measures.Cards
            .Select(c => new RectF(layout.Places[c.Type].X, layout.Places[c.Type].Y, c.Width, c.Height))
            .ToList();
        for (var i = 0; i < boxes.Count; i++)
        {
            for (var j = i + 1; j < boxes.Count; j++)
                Assert.False(boxes[i].Intersects(boxes[j]));
        }
        Assert.Equal(0f, boxes.Min(static b => b.Left), 0.01f);
        Assert.Equal(0f, boxes.Min(static b => b.Bottom), 0.01f);
        Assert.Equal(layout.Width, boxes.Max(static b => b.Right), 0.01f);
        Assert.Equal(layout.Height, boxes.Max(static b => b.Top), 0.01f);
    }

    [Fact]
    public void A_route_runs_from_its_source_card_to_an_arrowhead_whose_tip_touches_its_target()
    {
        var measures = Zoo4();

        var layout = TypeLayout.Arrange(measures);

        var route = layout.Routes.Single(r => r.Link == LionIsAnimal);
        var lion = Placed(layout, measures, Lion);
        var animal = Placed(layout, measures, Animal);
        Assert.True(IsOnBorder(route.Pieces[0].Start, lion.TopLeft, lion.Card));
        Assert.True(IsOnBorder(route.Tip, animal.TopLeft, animal.Card));
        var arrowBase = route.Pieces[^1].End;
        var length = MathF.Sqrt(arrowBase.DistanceSqTo(route.Tip));
        Assert.Equal(TypeLayout.ArrowLength, length, 0.5f);
    }

    [Fact]
    public void A_route_to_a_type_held_leaves_room_for_a_diamond_on_the_card_holding_it()
    {
        var measures = Zoo4();

        var layout = TypeLayout.Arrange(measures);

        var held = layout.Routes.Single(r => r.Link == KeeperHoldsAnimals);
        var keeper = Placed(layout, measures, Keeper);
        Assert.NotNull(held.SourceTip);
        Assert.True(IsOnBorder(held.SourceTip.Value, keeper.TopLeft, keeper.Card));
        var length = MathF.Sqrt(held.SourceTip.Value.DistanceSqTo(held.Pieces[0].Start));
        Assert.Equal(TypeLayout.DiamondLength, length, 0.5f);
        Assert.Null(layout.Routes.Single(r => r.Link == LionIsAnimal).SourceTip);
    }

    [Fact]
    public void A_labelled_link_has_its_label_placed_and_others_have_none()
    {
        var layout = TypeLayout.Arrange(Zoo4());

        Assert.NotNull(layout.Routes.Single(r => r.Link == KeeperHoldsAnimals).LabelCenter);
        Assert.Null(layout.Routes.Single(r => r.Link == ParrotIsAnimal).LabelCenter);
    }

    [Fact]
    public void A_link_to_a_card_not_measured_is_left_out()
    {
        var measures = new DiagramMeasures([new MeasuredCard(Lion, 100f, 50f)], [new MeasuredLink(LionIsAnimal, 0f, 0f)]);

        var layout = TypeLayout.Arrange(measures);

        Assert.Equal(new PointF(0f, 0f), layout.Places[Lion]);
        Assert.Empty(layout.Routes);
    }

    [Fact]
    public void A_type_holding_itself_is_routed_as_a_loop()
    {
        var self = new TypeLink(Keeper, Keeper, TypeLinkKind.HoldsOne, "Next");
        var measures = new DiagramMeasures([new MeasuredCard(Keeper, 100f, 50f)], [new MeasuredLink(self, 30f, 14f)]);

        var layout = TypeLayout.Arrange(measures);

        Assert.NotEmpty(Assert.Single(layout.Routes).Pieces);
    }

    [Fact]
    public void Nothing_to_lay_out_is_an_empty_layout()
    {
        var layout = TypeLayout.Arrange(new DiagramMeasures([], []));

        Assert.Empty(layout.Places);
        Assert.Equal(0f, layout.Width);
    }

    [Fact]
    public void Laying_out_stops_when_cancelled()
    {
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => TypeLayout.Arrange(Zoo4(), cancel.Token));
    }
}
