namespace ZGF.ILSpy.App.Tests.Fixtures;

public interface IDriven
{
    void Drive();
}

public abstract class Vehicle
{
    private int _mileage = 10;
    internal string Plate = "";

    public string Name { get; set; } = "";

    public Garage? Garage { get; set; }

    public abstract int Wheels { get; }

    /// <summary>How far the vehicle has gone.</summary>
    public int Mileage() => _mileage;
}

public class Car : Vehicle, IDriven
{
    public override int Wheels => 4;

    public void Drive()
    {
    }
}

public class Bike : Vehicle
{
    public override int Wheels => 2;
}

public class Garage
{
    public List<Vehicle> Vehicles { get; } = new();
}

public class Breakdown : Exception
{
    public int Code { get; set; }
}
