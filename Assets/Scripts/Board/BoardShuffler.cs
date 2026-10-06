using System;

public static class BoardShuffler
{
    // A failed search restores the exact original arrangement.
    public static bool TryShuffle(BoardModel board, Random random, int maxAttempts = 20, bool requireMatchProducingMove = false)
    {
        if (board == null) throw new ArgumentNullException(nameof(board));
        if (random == null) throw new ArgumentNullException(nameof(random));
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        var pieceList = new System.Collections.Generic.List<PieceData>();
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                if (!board.IsPlayable(x, y)) continue;
                var piece = board.GetCell(x, y).Piece;
                // Only occupied playable cells participate in the shuffle.
                if (piece == null || piece.IsMatchProtected) return false;
                if (!piece.IsMovableObjective) pieceList.Add(piece);
            }

        var original = pieceList.ToArray();
        var candidate = (PieceData[])original.Clone();
        bool committed = false;
        try
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                for (int i = candidate.Length - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var piece = candidate[i];
                    candidate[i] = candidate[j];
                    candidate[j] = piece;
                }
                Apply(board, candidate);
                if (MatchFinder.FindMatches(board).Count == 0 &&
                    (requireMatchProducingMove
                        ? BoardMoveFinder.FindMatchProducingMoves(board).Count > 0
                        : BoardMoveFinder.HasAnyValidMove(board)))
                {
                    committed = true;
                    return true;
                }
            }
            return false;
        }
        finally
        {
            if (!committed) Apply(board, original);
        }
    }

    private static void Apply(BoardModel board, PieceData[] pieces)
    {
        int index = 0;
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
                if (board.IsPlayable(x, y) && !board.GetCell(x, y).Piece.IsMovableObjective)
                    board.GetCell(x, y).Piece = pieces[index++];
    }
}
