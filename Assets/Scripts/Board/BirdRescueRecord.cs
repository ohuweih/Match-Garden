// Snapshot of a rescue, separate from scored/color-matched pieces.
public sealed class BirdRescueRecord
{
    public int BirdNumber { get; }
    public int X { get; }
    public int Y { get; }
    public long DistanceTraveled { get; }
    public BirdRescueGoal Goal { get; }

    public BirdRescueRecord(BoardCell cell)
    {
        BirdNumber = cell.Piece.BirdNumber;
        X = cell.X;
        Y = cell.Y;
        DistanceTraveled = cell.Piece.DistanceTraveled;
        Goal = cell.Piece.RescueGoal;
    }
}

public static class BirdRescueRules
{
    public static bool CanRescue(BoardCell cell, BoardModel board = null)
    {
        var piece = cell.Piece;
        if (piece != null && piece.IsMovableObjective && piece.RescueGoal?.Condition == BirdRescueCondition.ReachBottom)
        {
            if (board == null || !cell.IsPlayable) return false;
            // Ignore shape cutouts, but never mistake a lock or machine for an exit.
            for (int y = 0; y < cell.Y; y++)
            {
                var below = board.GetCell(cell.X, y);
                if (below.IsPlayable || below.IsLocked || below.Generator != null) return false;
            }
            return true;
        }
        return piece != null && piece.IsMovableObjective && piece.RescueGoal != null &&
            piece.RescueGoal.IsSatisfied(piece.DistanceTraveled, cell.X, cell.Y);
    }

    public static bool HasReadyBird(BoardModel board)
    {
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
                if (CanRescue(board.GetCell(x, y), board)) return true;
        return false;
    }
}
