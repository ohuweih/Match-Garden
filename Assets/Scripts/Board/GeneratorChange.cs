public sealed class GeneratorChange
{
    public int X { get; }
    public int Y { get; }
    public GeneratorType Type { get; }
    public int PreviousHits { get; }
    public int RemainingHits { get; }
    public bool Created => PreviousHits == 0 && RemainingHits > 0;
    public bool Destroyed => RemainingHits == 0;

    public GeneratorChange(int x, int y, GeneratorType type, int previousHits, int remainingHits)
    {
        X = x; Y = y; Type = type; PreviousHits = previousHits; RemainingHits = remainingHits;
    }
}
