using System.Collections.Generic;

public class MatchResult
{
    public MatchShape Shape { get; }

    public List<BoardCell> Cells { get; }

    public MatchResult(
        MatchShape shape,
        List<BoardCell> cells)
    {
        Shape = shape;
        Cells = cells;
    }
}