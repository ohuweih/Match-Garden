// Captured before removal: BoardCell references later hold refill pieces.
public sealed class ClearedPieceRecord
{
    public int X { get; }
    public int Y { get; }
    public PieceType Color { get; }
    public SpecialType Special { get; }

    public ClearedPieceRecord(BoardCell cell)
    {
        X = cell.X;
        Y = cell.Y;
        Color = cell.Piece.Color;
        Special = cell.Piece.Special;
    }
}
