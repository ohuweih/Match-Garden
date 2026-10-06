public class SpawnRecord
{
    public int X { get; }
    public int Y { get; }

    public PieceType Color { get; }
    public SpecialType Special { get; }

    public GravityDirection EntryDirection { get; }

    public SpawnRecord(
        int x,
        int y,
        PieceType color,
        SpecialType special,
        GravityDirection entryDirection =
            GravityDirection.Down)
    {
        X = x;
        Y = y;

        Color = color;
        Special = special;

        EntryDirection =
            entryDirection;
    }
}