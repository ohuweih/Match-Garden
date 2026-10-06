using System.Collections.Generic;

public static class BoardUnlocker
{
    public static List<LockedTileChange> ApplyMatches(BoardModel board, List<MatchResult> matches)
    {
        var changes = new List<LockedTileChange>();
        foreach (MatchResult match in matches)
        {
            var adjacent = new HashSet<BoardCell>();
            foreach (BoardCell cell in match.Cells)
            {
                AddLocked(board, cell.X - 1, cell.Y, adjacent);
                AddLocked(board, cell.X + 1, cell.Y, adjacent);
                AddLocked(board, cell.X, cell.Y - 1, adjacent);
                AddLocked(board, cell.X, cell.Y + 1, adjacent);
            }
            // A long or bent match can touch a lock on several sides, but counts once.
            foreach (BoardCell cell in adjacent)
                if (cell.RegisterAdjacentMatch())
                    changes.Add(new LockedTileChange(cell.X, cell.Y, cell.MatchesUntilUnlock));
        }
        return changes;
    }

    public static List<LockedTileChange> ApplyRound(
        BoardModel board, List<MatchResult> matches, List<BoardCell> clearedCells)
    {
        var changes = matches == null ? new List<LockedTileChange>() : ApplyMatches(board, matches);
        var matchedCells = new HashSet<BoardCell>();
        if (matches != null)
            foreach (var match in matches)
                foreach (var cell in match.Cells) matchedCells.Add(cell);

        // Extra clears come from special effects. Group all effects in this round
        // into one hit per lock, excluding cells already counted as normal matches.
        var adjacent = new HashSet<BoardCell>();
        foreach (var cell in clearedCells)
        {
            if (matchedCells.Contains(cell)) continue;
            AddLocked(board, cell.X - 1, cell.Y, adjacent);
            AddLocked(board, cell.X + 1, cell.Y, adjacent);
            AddLocked(board, cell.X, cell.Y - 1, adjacent);
            AddLocked(board, cell.X, cell.Y + 1, adjacent);
        }
        foreach (var cell in adjacent)
            if (cell.RegisterAdjacentMatch())
                changes.Add(new LockedTileChange(cell.X, cell.Y, cell.MatchesUntilUnlock));
        return changes;
    }

    private static void AddLocked(BoardModel board, int x, int y, HashSet<BoardCell> cells)
    {
        if (x >= 0 && x < board.Width && y >= 0 && y < board.Height)
        {
            BoardCell cell = board.GetCell(x, y);
            if (cell.IsLocked) cells.Add(cell);
        }
    }
}
