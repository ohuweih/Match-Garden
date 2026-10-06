public sealed class GeneratorActivation
{
    public int X { get; }
    public int Y { get; }
    public GeneratorType Type { get; }
    public GeneratorActivation(int x, int y, GeneratorType type) { X = x; Y = y; Type = type; }
}
