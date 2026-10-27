using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;

namespace ZGF.ILSpy.App.Tests;

public sealed class LayoutTransitionTests
{
    private const float HalfWay = 0.15f;
    private const float Past = 1f;

    private static readonly Lazy<LoadedAssembly> s_fixtures = new(() => new AssemblyList().Open(AppStateDriver.FixturePath));

    private static CodeReference TypeOf(string type) => s_fixtures.Value.Root.Child(AppStateDriver.FixturesNamespace, type).Reference!;

    private static readonly CodeReference Car = TypeOf("Car");
    private static readonly CodeReference Vehicle = TypeOf("Vehicle");
    private static readonly TypeLink CarIsVehicle = new(Car, Vehicle, TypeLinkKind.Inherits);

    private static TypeLayout Arrange(float vehicleHeight, bool withCar = true)
    {
        var cards = new List<MeasuredCard> { new(Vehicle, 120f, vehicleHeight) };
        if (withCar)
            cards.Add(new MeasuredCard(Car, 100f, 60f));
        return TypeLayout.Arrange(new DiagramMeasures(cards, [new MeasuredLink(CarIsVehicle, 0f, 0f)]));
    }

    [Fact]
    public void The_first_layout_is_drawn_at_once()
    {
        var layout = Arrange(80f);
        var transition = new LayoutTransition();

        Assert.True(transition.Follow(layout));

        Assert.False(transition.IsMoving);
        Assert.Equal(layout.Places[Car], transition.PlaceOf(Car));
        Assert.False(transition.Follow(layout));
    }

    [Fact]
    public void Cards_glide_from_their_old_places_to_their_new_ones()
    {
        var before = Arrange(80f);
        var after = Arrange(300f);
        var transition = new LayoutTransition();
        transition.Follow(before);

        transition.Follow(after);
        transition.Advance(HalfWay);

        Assert.True(transition.IsMoving);
        var expected = RoutePiece.Lerp(before.Places[Car], after.Places[Car], Ease.OutCubic(0.5f));
        var drawn = transition.PlaceOf(Car)!.Value;
        Assert.Equal(expected.X, drawn.X, 0.01f);
        Assert.Equal(expected.Y, drawn.Y, 0.01f);
        transition.Advance(Past);
        Assert.False(transition.IsMoving);
        Assert.Equal(after.Places[Car], transition.PlaceOf(Car));
    }

    [Fact]
    public void A_card_the_old_layout_lacked_appears_in_its_new_place()
    {
        var transition = new LayoutTransition();
        transition.Follow(Arrange(80f, withCar: false));
        var after = Arrange(80f);

        transition.Follow(after);
        transition.Advance(HalfWay);

        Assert.Equal(after.Places[Car], transition.PlaceOf(Car));
    }

    [Fact]
    public void A_route_bends_from_its_old_shape_and_ends_as_the_new_one()
    {
        var before = Arrange(80f);
        var after = Arrange(300f);
        var transition = new LayoutTransition();
        transition.Follow(before);
        transition.Follow(after);
        var route = after.Routes.Single();
        var old = before.Routes.Single();

        var starting = transition.Shape(route);

        Assert.Equal(Math.Max(route.Pieces.Count, old.Pieces.Count), starting.Pieces.Count);
        Assert.Equal(old.Pieces[0].Start, starting.Pieces[0].Start);
        Assert.Equal(old.Tip, starting.Tip);
        transition.Advance(Past);
        Assert.Same(route, transition.Shape(route));
    }
}
