public sealed class PackageChange
{
    public int X { get; }
    public int Y { get; }
    public bool Opened { get; }
    public PackageContent? Content { get; }
    // Null means the package became a stationary board object.
    public PieceSnapshot Piece { get; }

    public PackageChange(BoardCell cell, bool opened, PackageContent? content = null)
    {
        X = cell.X; Y = cell.Y; Opened = opened; Content = content;
        Piece = cell.IsEmpty ? null : new PieceSnapshot(cell.Piece);
    }
}
