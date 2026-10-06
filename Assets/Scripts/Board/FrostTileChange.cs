// An immutable snapshot: the board may finish several cascades before they animate.
public sealed class FrostTileChange
{
    public int X { get; }
    public int Y { get; }
    public int PreviousLayers { get; }
    public int RemainingLayers { get; }
    public int LayersRemoved => System.Math.Max(0, PreviousLayers - RemainingLayers);
    public int LayersAdded => System.Math.Max(0, RemainingLayers - PreviousLayers);

    public FrostTileChange(int x, int y, int previousLayers, int remainingLayers)
    {
        X = x;
        Y = y;
        PreviousLayers = previousLayers;
        RemainingLayers = remainingLayers;
    }
}
