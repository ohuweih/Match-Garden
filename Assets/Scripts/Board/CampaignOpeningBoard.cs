using System;
using System.Collections.Generic;

// Runs after terrain and birds are placed, before views or objective events exist.
public static class CampaignOpeningBoard
{
    public static bool TryPrepare(BoardModel board, int maxAttempts = 64)
    {
        if (board == null) throw new ArgumentNullException(nameof(board));
        var cells = new List<BoardCell>();
        var originals = new List<PieceData>();
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                var cell = board.GetCell(x, y);
                if (cell.IsPlayable && !cell.IsEmpty && cell.Piece.IsColored)
                { cells.Add(cell); originals.Add(cell.Piece); }
            }
        bool success = false;
        try
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                foreach (var cell in cells) cell.Piece = null;
                foreach (var cell in cells)
                {
                    // Preserve weighted color selection, rejecting triples AND 2x2 squares.
                    bool placed = false;
                    for (int roll = 0; roll < 24; roll++)
                    {
                        cell.Piece = board.GetRandomPiece();
                        if (MatchFinder.FindMatches(board).Count == 0) { placed = true; break; }
                    }
                    if (!placed)
                        foreach (PieceType color in Enum.GetValues(typeof(PieceType)))
                        {
                            cell.Piece = new PieceData(color);
                            if (MatchFinder.FindMatches(board).Count == 0) { placed = true; break; }
                        }
                    if (!placed) break;
                }
                if (cells.TrueForAll(cell => !cell.IsEmpty) &&
                    MatchFinder.FindMatches(board).Count == 0 && BoardMoveFinder.HasAnyValidMove(board))
                { success = true; return true; }
            }
            return false;
        }
        finally
        {
            if (!success)
                for (int i = 0; i < cells.Count; i++) cells[i].Piece = originals[i];
        }
    }
}
