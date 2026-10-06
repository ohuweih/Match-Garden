public sealed class LockedTileChange
{
    public int X { get; }
    public int Y { get; }
    public int RemainingMatches { get; }

    public LockedTileChange(int x, int y, int remainingMatches)
    {
        X = x;
        Y = y;
        RemainingMatches = remainingMatches;
    }
}
