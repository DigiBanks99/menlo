namespace Menlo.Common;

public record Liter : IUnit
{
    public string Name { get; } = "Liter";
    public string Symbol { get; } = "l";

    public static Liter Instance { get; } = new();

    public override string ToString()
    {
        return Symbol;
    }
}
